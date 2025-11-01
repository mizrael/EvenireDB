using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EvenireDB.Client;
using EvenireDB.Common;

namespace EvenireDB.Benchmark;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class RestApiBenchmark : IAsyncDisposable
{
    private readonly BenchmarkServerFixture _fixture;

    private BenchmarkServerFactory? _server;
    private HttpClient? _client;
    private IEventsClient? _eventsClient;

    private StreamId _streamId;
    private Client.EventData[] _events;

    public RestApiBenchmark()
    {
        _fixture = new BenchmarkServerFixture();
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new BenchmarkServerFactory();
        _client = _server.CreateClient();
        _eventsClient = new HttpEventsClient(_client);

        _streamId = new StreamId($"benchmark-stream-{this.EventSize}");
        _events = Enumerable.Range(0, this.EventsCount)
            .Select(i => Client.EventData.Create(new { id = i, data = new string('x', this.EventSize) }))
            .ToArray();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _client?.Dispose();

        if (_server != null)
            await _server.DisposeAsync();
    }

    [Params(10, 1024, 10240)]
    public int EventSize { get; set; }

    [Params(1, 10, 100)]
    public int EventsCount { get; set; }

    [Benchmark(Baseline = true)]
    public async Task WriteEvents()
    {
        await _eventsClient!.AppendAsync(_streamId, _events);
    }
}