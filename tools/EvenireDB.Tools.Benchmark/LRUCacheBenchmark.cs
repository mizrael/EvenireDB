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

    [Benchmark(Baseline = true)]
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
    public async Task ConcurrentMixedReadWrite()
    {
        // Pre-populate half
        for (int i = 0; i < CacheCapacity / 2; i++)
            await _cache.GetOrAddAsync(i, (k, _) => ValueTask.FromResult(k));

        var tasks = Enumerable.Range(0, ConcurrencyLevel).Select(t =>
            Task.Run(async () =>
            {
                for (int i = 0; i < CacheCapacity; i++)
                {
                    if (i % 3 == 0)
                        _cache.AddOrUpdate(i, i * t);
                    else
                        await _cache.GetOrAddAsync(i, (k, _) => ValueTask.FromResult(k));
                }
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
