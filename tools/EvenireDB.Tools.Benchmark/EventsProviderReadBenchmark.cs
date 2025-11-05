using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EvenireDB.Common;
using EvenireDB.Persistence;
using System.Text;

namespace EvenireDB.Benchmark;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class EventsProviderReadBenchmark
{
    private IExtentsProvider _extentsProvider;
    private IDataRepository _dataRepository;
    private IHeadersRepository _headersRepository;
    private IEventsProvider _eventsProvider;

    private string _extentsBasePath;

    private StreamId _streamId;

    [GlobalSetup]
    public async Task Setup()
    { 
        _extentsBasePath = Path.Combine(Path.GetTempPath(), "EvenireDB", "Benchmark", nameof(EventsProviderReadBenchmark), DateTimeOffset.UtcNow.UtcTicks.ToString());
        _extentsProvider = new ExtentsProvider(new(_extentsBasePath));
        _headersRepository = new HeadersRepository();
        _dataRepository = new DataRepository(this.BufferSize);
        _eventsProvider = new EventsProvider(_headersRepository, _dataRepository, _extentsProvider);

        _streamId = new StreamId(nameof(EventsProviderReadBenchmark));

        var events = Enumerable.Range(0, this.EventsCount)
                                .Select(i => new Event(new EventId(i, 0), "test", 
                                                       Encoding.UTF8.GetBytes(new string('x', Random.Shared.Next(131, this.MaxEventSize)))))
                                .ToArray();
        await _eventsProvider.AppendAsync(_streamId, events);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_extentsBasePath))
            Directory.Delete(_extentsBasePath, true);
    }

    [Benchmark(Baseline = true)]
    public async Task ReadEvents()
    {
        var events = _eventsProvider.ReadAsync(_streamId, 0, this.EventsCount);
        await foreach(var @event in events)
        {
            // No-op
        }
    }

    [Params(10240)]
    public int MaxEventSize { get; set; }

    [Params(592)]
    public int EventsCount { get; set; }

    [Params(4096, 65536, 262144, 1048576, 2097152)]
    public int BufferSize { get; set; }
}
