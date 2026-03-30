using EvenireDB.Common;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace EvenireDB.Tests;

public class EventsWriterConcurrencyTests
{
    private static readonly byte[] _defaultData = new byte[] { 0x42 };

    [Fact]
    public async Task AppendAsync_should_not_lose_events_under_concurrent_writes()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedEvents = new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        var capturedBatches = new ConcurrentBag<IncomingEventsBatch>();
        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Do<IncomingEventsBatch>(b => capturedBatches.Add(b))).Returns(true);

        var idGenerator = new EventIdGenerator(TimeProvider.System);
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        const int writerCount = 10;
        const int eventsPerWriter = 20;

        var tasks = Enumerable.Range(0, writerCount).Select(w =>
        {
            var events = Enumerable.Range(0, eventsPerWriter)
                .Select(i => new EventData($"type-{w}", _defaultData))
                .ToArray();
            return sut.AppendAsync(streamId, events).AsTask();
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.IsType<SuccessResult>(r));
        Assert.Equal(writerCount * eventsPerWriter, capturedBatches.Sum(b => b.Events.Count()));
    }

    [Fact]
    public async Task AppendAsync_should_maintain_id_ordering_under_concurrent_writes()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedEvents = new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        var capturedBatches = new ConcurrentBag<IncomingEventsBatch>();
        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Do<IncomingEventsBatch>(b => capturedBatches.Add(b))).Returns(true);

        var idGenerator = new EventIdGenerator(TimeProvider.System);
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        const int writerCount = 5;
        const int eventsPerWriter = 10;

        var tasks = Enumerable.Range(0, writerCount).Select(_ =>
        {
            var events = Enumerable.Range(0, eventsPerWriter)
                .Select(i => new EventData("lorem", _defaultData))
                .ToArray();
            return sut.AppendAsync(streamId, events).AsTask();
        }).ToArray();

        await Task.WhenAll(tasks);

        foreach (var batch in capturedBatches)
        {
            var events = batch.Events.ToList();
            for (int i = 1; i < events.Count; i++)
            {
                var prev = events[i - 1].Id;
                var curr = events[i].Id;
                bool isOrdered = curr.Timestamp > prev.Timestamp ||
                    (curr.Timestamp == prev.Timestamp && curr.Sequence > prev.Sequence);
                Assert.True(isOrdered,
                    $"Event at index {i} breaks ordering: ({prev.Timestamp},{prev.Sequence}) -> ({curr.Timestamp},{curr.Sequence})");
            }
        }
    }

    [Fact]
    public async Task AppendAsync_should_allow_concurrent_writes_with_same_expected_version()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedEvents = new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        var capturedBatches = new ConcurrentBag<IncomingEventsBatch>();
        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Do<IncomingEventsBatch>(b => capturedBatches.Add(b))).Returns(true);

        var idGenerator = new EventIdGenerator(TimeProvider.System);
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var events = new[] { new EventData("lorem", _defaultData) };

        var task1 = sut.AppendAsync(streamId, events, expectedVersion: 0).AsTask();
        var task2 = sut.AppendAsync(streamId, events, expectedVersion: 0).AsTask();

        var results = await Task.WhenAll(task1, task2);

        // Both succeed since neither modifies the cached list — the persistence layer handles ordering
        Assert.All(results, r => Assert.IsType<SuccessResult>(r));
        Assert.Equal(2, capturedBatches.Count);
    }
}
