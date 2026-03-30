using EvenireDB.Common;
using System.Threading.Channels;

namespace EvenireDB.Tests;

public class EventsWriterConcurrencyTests
{
    private static readonly byte[] _defaultData = new byte[] { 0x42 };

    [Fact]
    public async Task AppendAsync_should_not_lose_events_under_concurrent_writes()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedList = new List<Event>();
        var cachedEvents = new CachedEvents(cachedList, new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Any<IncomingEventsBatch>()).Returns(true);

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
        Assert.Equal(writerCount * eventsPerWriter, cachedList.Count);
    }

    [Fact]
    public async Task AppendAsync_should_maintain_id_ordering_under_concurrent_writes()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedList = new List<Event>();
        var cachedEvents = new CachedEvents(cachedList, new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Any<IncomingEventsBatch>()).Returns(true);

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

        for (int i = 1; i < cachedList.Count; i++)
        {
            var prev = cachedList[i - 1].Id;
            var curr = cachedList[i].Id;
            bool isOrdered = curr.Timestamp > prev.Timestamp ||
                (curr.Timestamp == prev.Timestamp && curr.Sequence > prev.Sequence);
            Assert.True(isOrdered,
                $"Event at index {i} breaks ordering: ({prev.Timestamp},{prev.Sequence}) -> ({curr.Timestamp},{curr.Sequence})");
        }
    }

    [Fact]
    public async Task AppendAsync_should_handle_version_check_correctly_under_concurrency()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedList = new List<Event>();
        var cachedEvents = new CachedEvents(cachedList, new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Any<IncomingEventsBatch>()).Returns(true);

        var idGenerator = new EventIdGenerator(TimeProvider.System);
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var events = new[] { new EventData("lorem", _defaultData) };

        // Two concurrent appends both expecting version 0 — only one should succeed
        var task1 = sut.AppendAsync(streamId, events, expectedVersion: 0).AsTask();
        var task2 = sut.AppendAsync(streamId, events, expectedVersion: 0).AsTask();

        var results = await Task.WhenAll(task1, task2);

        var successes = results.OfType<SuccessResult>().Count();
        var failures = results.OfType<FailureResult>().Count();

        Assert.Equal(1, successes);
        Assert.Equal(1, failures);
    }
}
