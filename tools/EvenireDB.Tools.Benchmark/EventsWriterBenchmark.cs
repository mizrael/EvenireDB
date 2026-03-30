using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EvenireDB.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Threading.Channels;

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
