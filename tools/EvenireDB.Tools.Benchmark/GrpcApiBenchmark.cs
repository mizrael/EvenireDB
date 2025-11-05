using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EvenireDB.Client;
using EvenireDB.Common;
using Grpc.Net.Client;

namespace EvenireDB.Benchmark;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class GrpcApiBenchmark : IAsyncDisposable
{
    private readonly BenchmarkServerFixture _fixture;

    private BenchmarkServerFactory? _server;
    private GrpcChannel? _grpcChannel;
    private IEventsClient? _eventsClient;

    private StreamId _streamId;
    private Client.EventData[] _events;

    public GrpcApiBenchmark()
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

        _grpcChannel = _server.CreateGrpcChannel();
        var client = new GrpcEvents.EventsGrpcService.EventsGrpcServiceClient(_grpcChannel);
        _eventsClient = new GrpcEventsClient(client);

        _streamId = new StreamId($"benchmark-stream-{this.MaxEventSize}");
        _events = Utils.CreateClientEvents(this.EventsCount, this.MaxEventSize / 2, this.MaxEventSize);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _grpcChannel?.Dispose();

        if (_server != null)
            await _server.DisposeAsync();
    }

    [Params(10240)]
    public int MaxEventSize { get; set; }

    [Params(10, 100, 1000)]
    public int EventsCount { get; set; }

    [Benchmark(Baseline = true)]
    public async Task WriteEvents()
    {
        await _eventsClient!.AppendAsync(_streamId, _events);
    }
}

