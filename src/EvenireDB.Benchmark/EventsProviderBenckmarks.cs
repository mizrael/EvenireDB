using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using EvenireDB;
using EvenireDB.Common;
using EvenireDB.Extents;
using EvenireDB.Persistence;
using EvenireDB.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Channels;

public class EventsProviderBenckmarks
{
    private readonly static byte[] _data = Enumerable.Repeat((byte)42, 100).ToArray();
    private readonly static Guid _streamId = Guid.NewGuid();
    private readonly Consumer _consumer = new Consumer();

    private EventsReader _sut;

    [Params(10, 100, 1000)]
    public uint EventsCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var basePath = AppContext.BaseDirectory;
        var dataPath = Path.Combine(basePath, "data");

        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        var repoConfig = new FileEventsRepositoryConfig();
        var extentInfoProvider = new ExtentInfoProvider(new ExtentInfoProviderConfig(dataPath));
        var dataRepo = new DataRepository();
        var headersRepo = new HeadersRepository();
        var repo = new EventsProvider(headersRepo, dataRepo, extentInfoProvider);

        var cache = new EventsCache(
            new NullLogger<EventsCache>(),
            new LRUCache<Guid, CachedEvents>(this.EventsCount),
            repo);

        var logger = new NullLogger<EventsReader>();

        _sut = new EventsReader(EventsReaderConfig.Default, cache);

        var events = Enumerable.Range(0, (int)this.EventsCount).Select(i => new Event(new EventId(i, 0), "lorem", _data)).ToArray();
        Task.WaitAll(repo.AppendAsync(_streamId, events).AsTask());
    }

    [Benchmark(Baseline = true)]    
    public async Task ReadAsync_Baseline()
    {
        int count = 0;
        var events = await _sut.ReadAsync(_streamId, StreamPosition.Start).ToListAsync().ConfigureAwait(false);
        foreach (var @evt in events)
            count++;
    }
}