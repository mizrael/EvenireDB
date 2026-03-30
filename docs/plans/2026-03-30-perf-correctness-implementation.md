# Performance & Correctness Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix critical correctness and safety bugs in EvenireDB's hot paths: LRUCache thread safety, shared mutable cache between readers/writers, sync waits in async methods, and persistence worker cleanup. Add benchmarks for affected components.

**Architecture:** Characterization-test-first approach. For each component: verify existing behavior with tests, refactor, verify tests still pass, add new tests for new behavior. Changes are isolated per component to minimize risk.

**Tech Stack:** .NET 10, xUnit, NSubstitute, BenchmarkDotNet

---

### Task 1: LRUCache — Add Characterization Tests for Concurrency

Before touching the LRUCache implementation, ensure we have tests that verify current behavior under concurrency so we can detect regressions during refactoring.

**Files:**
- Modify: `tests/EvenireDB.Tests/LRUCacheTests.cs`

**Tests to add (append to existing class):**

```csharp
    [Fact]
    public async Task GetOrAddAsync_should_handle_concurrent_reads_on_different_keys()
    {
        var sut = new LRUCache<string, int>(100);

        // Pre-populate
        for (int i = 0; i < 50; i++)
            await sut.GetOrAddAsync($"key-{i}", (_, _) => ValueTask.FromResult(i));

        // Concurrent reads on existing keys
        var tasks = Enumerable.Range(0, 50).Select(i =>
            sut.GetOrAddAsync($"key-{i}", (_, _) => ValueTask.FromResult(-1)).AsTask()
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        for (int i = 0; i < 50; i++)
            Assert.Equal(i, results[i]);
    }

    [Fact]
    public async Task AddOrUpdate_should_handle_concurrent_updates_to_same_key()
    {
        var sut = new LRUCache<string, int>(10);
        await sut.GetOrAddAsync("key", (_, _) => ValueTask.FromResult(0));

        var tasks = Enumerable.Range(1, 20).Select(i =>
            Task.Run(() => sut.AddOrUpdate("key", i))
        ).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1u, sut.Count);
        var result = await sut.GetOrAddAsync("key", (_, _) => ValueTask.FromResult(-1));
        Assert.InRange(result, 1, 20);
    }

    [Fact]
    public async Task Remove_should_handle_concurrent_removes()
    {
        var sut = new LRUCache<string, int>(100);

        for (int i = 0; i < 50; i++)
            await sut.GetOrAddAsync($"key-{i}", (_, _) => ValueTask.FromResult(i));

        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() => sut.Remove($"key-{i}"))
        ).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(0u, sut.Count);
    }

    [Fact]
    public async Task GetOrAddAsync_should_not_invoke_factory_twice_for_same_key_concurrently()
    {
        var sut = new LRUCache<string, int>(10);
        int factoryCallCount = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ =>
            sut.GetOrAddAsync("key", async (_, _) =>
            {
                Interlocked.Increment(ref factoryCallCount);
                await Task.Delay(50);
                return 42;
            }).AsTask()
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, factoryCallCount);
        Assert.All(results, r => Assert.Equal(42, r));
    }
```

**Build & run:**
```shell
cd src && dotnet build --nologo -v q
dotnet test ../tests/EvenireDB.Tests/EvenireDB.Tests.csproj --no-build --filter "FullyQualifiedName~LRUCacheTests"
```

**Commit:** `test: add LRUCache concurrency characterization tests`

---

### Task 2: LRUCache — Refactor to ReaderWriterLockSlim + Async Waits

Replace the patchwork of locks and synchronous semaphore waits with a single `ReaderWriterLockSlim` and async-safe patterns. Key changes:
- Single `ReaderWriterLockSlim` replaces `_moveToHeadLock`, `_dropLock`, `_getSemaphoresLock`
- All dictionary/linked-list mutations happen under write lock
- `ContainsKey` and cache-hit reads happen under read lock
- Per-key semaphores remain only for async factory deduplication in `GetOrAddAsync`
- All `SemaphoreSlim.Wait()` become `WaitAsync()` with `try/finally`
- `AddOrUpdate` becomes async to support `WaitAsync`

**Files:**
- Modify: `src/EvenireDB/Utils/ICache.cs` — make `AddOrUpdate` async
- Modify: `src/EvenireDB/Utils/LRUCache.cs` — full refactor
- Modify: `src/EvenireDB/StreamsCache.cs` — adapt to async `AddOrUpdate`

**ICache.cs — make AddOrUpdate async:**

```csharp
namespace EvenireDB.Utils;

public interface ICache<TKey, TValue> where TKey : notnull
{
    bool ContainsKey(TKey key);
    ValueTask<TValue> GetOrAddAsync(TKey key, Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default);
    ValueTask AddOrUpdateAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
    void DropOldest(uint maxCount);
    void Remove(TKey key);

    public uint Count { get; }
    IEnumerable<TKey> Keys { get; }
}
```

**LRUCache.cs — full replacement:**

Replace the entire class body with a thread-safe implementation using `ReaderWriterLockSlim` for dictionary/list access and `SemaphoreSlim.WaitAsync()` for factory deduplication. Key design:

- `_rwLock` (ReaderWriterLockSlim) protects `_cache`, `_head`, `_tail` linked list
- `_factorySemaphores` (Dictionary + lock) provides per-key dedup for async factory calls only
- `ContainsKey` takes read lock only
- `GetOrAddAsync` takes read lock to check cache hit, upgrades to write lock on miss
- `AddOrUpdateAsync` takes write lock
- `Remove` takes write lock
- `DropOldest` takes write lock
- `MoveToHead` called only within write lock (no separate lock needed)
- All semaphore waits use `WaitAsync` with `try/finally`

**StreamsCache.cs — adapt Update call:**

Change `Update` to call the new async method:

```csharp
    public ValueTask UpdateAsync(StreamId streamId, CachedEvents entry, CancellationToken cancellationToken = default)
    => _cache.AddOrUpdateAsync(streamId, entry, cancellationToken);
```

And update `IStreamsCache` interface accordingly.

**Build & run all tests:**
```shell
cd src && dotnet build --nologo -v q
dotnet test ../tests/EvenireDB.Tests/EvenireDB.Tests.csproj --no-build --filter "FullyQualifiedName~LRUCacheTests|FullyQualifiedName~StreamsCacheTests"
```

Ensure all existing + new characterization tests pass.

**Commit:** `refactor: make LRUCache thread-safe with ReaderWriterLockSlim`

---

### Task 3: EventsWriter — Cache Invalidation on Write + Async Semaphore

Change `EventsWriter` to invalidate the cache after writing to the channel, instead of mutating the cached list in place. Also replace the sync `Semaphore.Wait()` with `await WaitAsync()`.

**Files:**
- Modify: `src/EvenireDB/EventsWriter.cs`
- Modify: `src/EvenireDB/IStreamsCache.cs` — remove `Update` method (no longer needed)
- Modify: `src/EvenireDB/StreamsCache.cs` — remove `Update` method
- Modify: `tests/EvenireDB.Tests/EventsWriterTests.cs` — update tests that verify cache mutation
- Modify: `tests/EvenireDB.Tests/EventsWriterConcurrencyTests.cs` — update tests
- Modify: `tests/EvenireDB.Tests/StreamsCacheTests.cs` — remove `Update_should_replace_cached_entry` test

**EventsWriter.cs — key changes:**

```csharp
    public async ValueTask<IOperationResult> AppendAsync(
        StreamId streamId,
        IEnumerable<EventData> incomingEvents,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (incomingEvents is null)
            return FailureResult.NullEvents(streamId);

        if (!incomingEvents.Any())
            return new SuccessResult();

        CachedEvents entry = await _cache.GetEventsAsync(streamId, cancellationToken).ConfigureAwait(false);

        if (expectedVersion.HasValue && entry.Events.Count != expectedVersion)
            return FailureResult.VersionMismatch(streamId, expectedVersion.Value, entry.Events.Count);

        await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var newEventsCount = incomingEvents.Count();
            _logger.AppendingEventsToStream(newEventsCount, streamId);

            var events = new List<Event>(newEventsCount);

            EventId? previousEventId = null;
            var lastEvent = entry.Events.LastOrDefault();
            if (lastEvent != null)
                previousEventId = lastEvent.Id;

            for (int i = 0; i != newEventsCount; i++)
            {
                var eventData = incomingEvents.ElementAt(i);
                var eventId = _idGenerator.Generate(previousEventId);
                var @event = new Event(eventId, eventData.Type, eventData.Data);
                events.Add(@event);
                previousEventId = eventId;
            }

            var group = new IncomingEventsBatch(streamId, events);
            if (!_writer.TryWrite(group))
                return FailureResult.CannotInitiateWrite(streamId);

            // Invalidate cache — next reader will reload from disk
            _cache.Remove(streamId);
        }
        finally
        {
            entry.Semaphore.Release();
        }

        return new SuccessResult();
    }
```

**Test updates:**
- Tests that assert `cachedList.Count` after append (e.g., `AppendAsync_should_add_events_to_cache_on_success`) need to be updated — the writer no longer adds to the cache. Instead verify the channel received the correct batch and cache was invalidated.
- Concurrency tests need similar adjustments — verify no event loss via channel writes rather than cache list inspection.

**Build & run:**
```shell
cd src && dotnet build --nologo -v q
dotnet test ../tests/EvenireDB.Tests/EvenireDB.Tests.csproj --no-build --filter "FullyQualifiedName~EventsWriterTests|FullyQualifiedName~EventsWriterConcurrencyTests|FullyQualifiedName~StreamsCacheTests"
```

**Commit:** `refactor: invalidate cache on write instead of mutating shared list`

---

### Task 4: IncomingEventsPersistenceWorker — Cleanup

Remove the unnecessary `Task.Run` wrapper and the redundant outer `while` loop. Add `ConfigureAwait(false)`.

**Files:**
- Modify: `src/EvenireDB/Workers/IncomingEventsPersistenceWorker.cs`

**New implementation:**

```csharp
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var batch in _reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (batch is null)
                continue;
            try
            {
                await _repo.AppendAsync(batch.StreamId, batch.Events, cancellationToken)
                           .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.EventsGroupPersistenceError(batch.StreamId, ex.Message);
            }
        }
    }
```

Remove the `ExecuteAsyncCore` private method entirely.

**Build & run all tests:**
```shell
cd src && dotnet build --nologo -v q
cd src && dotnet test ./EvenireDb.Tests.slnf --no-build -m:1 -s ../tests.runsettings
```

**Commit:** `refactor: simplify persistence worker, remove Task.Run and redundant loop`

---

### Task 5: Full Integration Test Pass

Run the complete test suite to verify all changes work together end-to-end (server integration tests, client tests, etc.).

**Build & run:**
```shell
cd src && dotnet build --nologo -v q
cd src && dotnet test ./EvenireDb.Tests.slnf --no-build -m:1 -s ../tests.runsettings
```

All 135+ tests must pass with 0 failures.

**Commit:** only if any test fixups were needed.

---

### Task 6: Add Benchmarks for Affected Components

Add BenchmarkDotNet benchmarks targeting the components we refactored.

**Files:**
- Create: `tools/EvenireDB.Tools.Benchmark/LRUCacheBenchmark.cs`
- Create: `tools/EvenireDB.Tools.Benchmark/EventsWriterBenchmark.cs`

**LRUCacheBenchmark.cs:**

Benchmark concurrent `GetOrAddAsync` throughput (N tasks hitting the cache simultaneously), `AddOrUpdate` under contention, and `DropOldest` cost. Use `[Params]` to vary concurrency level and cache size.

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EvenireDB.Utils;

namespace EvenireDB.Benchmark;

[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class LRUCacheBenchmark
{
    private LRUCache<int, int> _cache = null!;

    [Params(100, 1000)]
    public int CacheCapacity { get; set; }

    [Params(4, 16)]
    public int ConcurrencyLevel { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _cache = new LRUCache<int, int>((uint)CacheCapacity);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cache?.Dispose();
    }

    [Benchmark]
    public async Task ConcurrentGetOrAdd()
    {
        var tasks = Enumerable.Range(0, ConcurrencyLevel).Select(t =>
            Task.Run(async () =>
            {
                for (int i = 0; i < CacheCapacity; i++)
                    await _cache.GetOrAddAsync(i, (k, _) => ValueTask.FromResult(k));
            })
        ).ToArray();

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentAddOrUpdate()
    {
        // Pre-populate
        for (int i = 0; i < CacheCapacity; i++)
            await _cache.GetOrAddAsync(i, (k, _) => ValueTask.FromResult(k));

        var tasks = Enumerable.Range(0, ConcurrencyLevel).Select(t =>
            Task.Run(async () =>
            {
                for (int i = 0; i < CacheCapacity; i++)
                    await _cache.AddOrUpdateAsync(i, i * t);
            })
        ).ToArray();

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task PopulateAndDropOldest()
    {
        for (int i = 0; i < CacheCapacity; i++)
            await _cache.GetOrAddAsync(i, (k, _) => ValueTask.FromResult(k));

        _cache.DropOldest((uint)(CacheCapacity / 3));
    }
}
```

**EventsWriterBenchmark.cs:**

Benchmark the append path with mocked cache and channel writer, measuring throughput of ID generation + event creation + channel write.

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EvenireDB.Common;
using NSubstitute;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace EvenireDB.Benchmark;

[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class EventsWriterBenchmark
{
    private EventsWriter _writer = null!;
    private StreamId _streamId;
    private EventData[] _events = null!;

    [Params(10, 100, 500)]
    public int EventsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _streamId = new StreamId { Key = Guid.NewGuid(), Type = "benchmark" };

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(_streamId, Arg.Any<CancellationToken>())
             .Returns(new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1)));

        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Any<IncomingEventsBatch>()).Returns(true);

        var idGenerator = new EventIdGenerator(TimeProvider.System);
        var logger = Substitute.For<ILogger<EventsWriter>>();

        _writer = new EventsWriter(cache, channelWriter, idGenerator, logger);

        _events = Enumerable.Range(0, EventsCount)
            .Select(i => new EventData("benchmark-type", new byte[] { 0x42 }))
            .ToArray();
    }

    [Benchmark]
    public async Task AppendEvents()
    {
        await _writer.AppendAsync(_streamId, _events);
    }
}
```

**Build & verify benchmarks compile:**
```shell
cd src && dotnet build --nologo -v q -c Release
```

**Capture baselines:**
```shell
./scripts/benchmark-compare.sh --update-baseline --filter "*LRUCacheBenchmark*"
```

**Commit:** `perf: add LRUCache and EventsWriter benchmarks`

---

## Execution Order & Dependencies

Tasks are sequential:
1. **Task 1** — Characterization tests (safety net)
2. **Task 2** — LRUCache refactor (depends on Task 1 tests passing)
3. **Task 3** — EventsWriter cache invalidation (depends on Task 2 for async AddOrUpdate)
4. **Task 4** — Persistence worker cleanup (independent, but run after Task 3)
5. **Task 5** — Full integration verification
6. **Task 6** — Benchmarks (run last, after all refactoring is stable)
