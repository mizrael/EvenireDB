using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using EvenireDB;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Channels;

public class EventsProviderBenckmarks
{
    private readonly static byte[] _data = Enumerable.Repeat((byte)42, 100).ToArray();
    private readonly static Guid _streamId = Guid.NewGuid();
    private readonly Consumer _consumer = new Consumer();

    private EventsProvider _sut;

    [Params(10, 100, 1000)]
    public int EventsCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var basePath = AppContext.BaseDirectory;
        var dataPath = Path.Combine(basePath, "data");

        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        var factory = new EventFactory(500_000);
        var repoConfig = new FileEventsRepositoryConfig(dataPath);
        var repo = new FileEventsRepository(repoConfig, factory);

        var options = new MemoryCacheOptions();
        var cache = new MemoryCache(options);

        var channel = Channel.CreateUnbounded<IncomingEventsGroup>();

        _sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

        var events = Enumerable.Range(0, this.EventsCount).Select(i => factory.Create(Guid.NewGuid(), "lorem", _data)).ToArray();
        Task.WaitAll(_sut.AppendAsync(_streamId, events).AsTask());
    }

    [Benchmark(Baseline = true)]    
    public async Task GetPageAsync_Baseline()
    {
        (await _sut.ReadAsync(_streamId).ConfigureAwait(false)).Consume(_consumer);
    }
}