# Performance & Correctness Fixes Design

**Date:** 2026-03-30
**Status:** Approved

## Problem

EvenireDB is transitioning from PoC to a proper project. A comprehensive performance analysis identified critical correctness and safety bugs in the hot paths that must be fixed before further development.

## Scope

Five areas, all focused on correctness and safety under concurrency:

1. LRUCache thread safety
2. Shared mutable cache between readers/writers
3. Sync waits blocking thread-pool threads in async methods
4. Persistence worker cleanup
5. Benchmarks for affected components

## Design

### 1. LRUCache Thread Safety

**Current state:** `Dictionary<TKey, Node>` accessed concurrently without consistent locking. Per-key `SemaphoreSlim.Wait()` blocks thread-pool threads. Missing `try/finally` around semaphore releases can deadlock. Global `_moveToHeadLock` serializes all cache hits.

**Fix:** Replace patchwork locking with `ReaderWriterLockSlim`. Read operations (`ContainsKey`, cache-hit path of `GetOrAddAsync`) take a read lock. Mutations (`Add`, `Remove`, `DropOldest`, `MoveToHead`) take a write lock with short critical sections. Replace `SemaphoreSlim.Wait()` with `WaitAsync()`. Wrap all semaphore operations in `try/finally`. Keep per-key semaphores only for async factory deduplication in `GetOrAddAsync`.

**Process:** Write characterization tests first → refactor → verify existing tests pass → add new concurrency tests.

### 2. Cache Invalidation on Write

**Current state:** `CachedEvents` holds a `List<Event>` that `EventsWriter` mutates via `AddRange()` while `EventsReader` iterates it concurrently without locking. Both a correctness bug and a performance hazard.

**Fix:** After `EventsWriter` successfully writes to the persistence channel, invalidate (remove) the stream's cache entry. The next reader triggers a cache miss, reloads from disk via `StreamsCache.Factory`, and gets a fresh consistent view. Remove `entry.Events.AddRange()` and `_cache.Update()` from the write path entirely.

**Trade-off:** First read after a write pays a disk I/O cost. This is correct by construction and matches standard database behavior.

### 3. Async Waits

**Current state:** `EventsWriter.AppendAsync()` calls `entry.Semaphore.Wait()` (blocking). `LRUCache` methods use synchronous `Wait()`. Both block thread-pool threads in async contexts → thread-pool starvation under load.

**Fix:** Replace all `SemaphoreSlim.Wait()` with `await SemaphoreSlim.WaitAsync().ConfigureAwait(false)`. Add `try/finally` around all acquire/release pairs. LRUCache changes are part of Section 1.

### 4. Persistence Worker Cleanup

**Current state:** `IncomingEventsPersistenceWorker.ExecuteAsync` wraps work in unnecessary `Task.Run` and has a redundant outer `while` loop around `ReadAllAsync`.

**Fix:** Remove `Task.Run` wrapper — call the core logic directly. Remove the outer `while` loop — `ReadAllAsync` already loops until channel completion or cancellation. Add `ConfigureAwait(false)` on the `await foreach`. Keep single consumer (multiple consumers would add ordering complexity with marginal benefit since disk I/O is sequential).

### 5. Benchmarks

Add BenchmarkDotNet benchmarks for affected components in `tools/EvenireDB.Tools.Benchmark/`:

- **LRUCacheBenchmark** — concurrent `GetOrAddAsync` throughput, contended `AddOrUpdate`, `DropOldest` cost
- **EventsWriterBenchmark** — append throughput (single stream, multiple streams), version checking overhead
- **ReadAfterWriteBenchmark** — write-then-read full path including cache-miss reload

Capture baselines before refactoring, re-measure after.

## Out of Scope

- DataRepository I/O batching (separate optimization pass)
- Multiple persistence consumers
- ExtentsProvider multi-extent sharding
- HttpEventsClient streaming
- Per-event allocation reduction (Event/EventData as value types)
