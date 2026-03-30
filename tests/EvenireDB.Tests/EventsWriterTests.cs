using EvenireDB.Common;
using System.Threading.Channels;

namespace EvenireDB.Tests;

public class EventsWriterTests
{
    private readonly static byte[] _defaultData = new byte[] { 0x42 };

    [Fact]
    public async Task AppendAsync_should_fail_when_stream_version_mismatch()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var inputEvents = Enumerable.Range(0, 10)
            .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
            .ToArray();

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1)));

        var channelWriter = NSubstitute.Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Any<IncomingEventsBatch>()).Returns(true);

        var idGenerator = Substitute.For<IEventIdGenerator>();
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var result = await sut.AppendAsync(streamId, inputEvents, expectedVersion: inputEvents.Count() - 1);

        Assert.IsType<FailureResult>(result);

        var failure = (FailureResult)result;
        Assert.Equal(ErrorCodes.VersionMismatch, failure.Code);
    }

    [Fact]
    public async Task AppendAsync_should_fail_when_channel_rejects_message()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var inputEvents = Enumerable.Range(0, 10)
            .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
            .ToArray();

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(new CachedEvents(new List<Event>(), new SemaphoreSlim(1,1)));
       
        var channelWriter = NSubstitute.Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Any<IncomingEventsBatch>()).Returns(false);

        var idGenerator = Substitute.For<IEventIdGenerator>();
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);
        
        await sut.AppendAsync(streamId, inputEvents);
        var result = await sut.AppendAsync(streamId, inputEvents);
        Assert.IsType<FailureResult>(result);

        var failure = (FailureResult)result;
        Assert.Equal(ErrorCodes.CannotInitiateWrite, failure.Code);
    }

    [Fact]
    public async Task AppendAsync_should_succeed_when_events_valid()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var expectedEvents = Enumerable.Range(0, 242)
            .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
            .ToArray();            
        
        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1)));

        var channelWriter = NSubstitute.Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Any<IncomingEventsBatch>()).Returns(true);

        var idGenerator = Substitute.For<IEventIdGenerator>();
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var result = await sut.AppendAsync(streamId, expectedEvents);
        Assert.IsType<SuccessResult>(result);
    }

    [Fact]
    public async Task AppendAsync_should_return_success_for_empty_events()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var cache = Substitute.For<IStreamsCache>();
        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        var idGenerator = Substitute.For<IEventIdGenerator>();
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var result = await sut.AppendAsync(streamId, Enumerable.Empty<EventData>());

        Assert.IsType<SuccessResult>(result);
        await cache.DidNotReceive().GetEventsAsync(Arg.Any<StreamId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AppendAsync_should_return_failure_for_null_events()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var cache = Substitute.For<IStreamsCache>();
        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        var idGenerator = Substitute.For<IEventIdGenerator>();
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var result = await sut.AppendAsync(streamId, null!);

        Assert.IsType<FailureResult>(result);
        Assert.Equal(ErrorCodes.BadRequest, ((FailureResult)result).Code);
    }

    [Fact]
    public async Task AppendAsync_should_invalidate_cache_on_success()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedEvents = new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        IncomingEventsBatch? capturedBatch = null;
        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Do<IncomingEventsBatch>(b => capturedBatch = b)).Returns(true);

        var idGenerator = new EventIdGenerator(TimeProvider.System);
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var inputEvents = new[]
        {
            new EventData("typeA", new byte[] { 0x01 }),
            new EventData("typeB", new byte[] { 0x02 }),
        };

        var result = await sut.AppendAsync(streamId, inputEvents);

        Assert.IsType<SuccessResult>(result);
        Assert.NotNull(capturedBatch);
        var batchEvents = capturedBatch.Events.ToList();
        Assert.Equal(2, batchEvents.Count);
        Assert.Equal("typeA", batchEvents[0].Type);
        Assert.Equal("typeB", batchEvents[1].Type);
        cache.Received(1).Remove(streamId);
    }

    [Fact]
    public async Task AppendAsync_should_write_batch_to_channel_on_success()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedEvents = new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Any<IncomingEventsBatch>()).Returns(true);

        var idGenerator = new EventIdGenerator(TimeProvider.System);
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var inputEvents = new[] { new EventData("typeA", new byte[] { 0x01 }) };

        await sut.AppendAsync(streamId, inputEvents);

        channelWriter.Received(1).TryWrite(Arg.Is<IncomingEventsBatch>(b =>
            b.StreamId == streamId && b.Events.Count() == 1));
    }

    [Fact]
    public async Task AppendAsync_should_generate_sequential_ids_for_events()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var cachedEvents = new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        IncomingEventsBatch? capturedBatch = null;
        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Do<IncomingEventsBatch>(b => capturedBatch = b)).Returns(true);

        var idGenerator = new EventIdGenerator(TimeProvider.System);
        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var inputEvents = Enumerable.Range(0, 5)
            .Select(i => new EventData("lorem", new byte[] { 0x42 }))
            .ToArray();

        await sut.AppendAsync(streamId, inputEvents);

        Assert.NotNull(capturedBatch);
        var events = capturedBatch.Events.ToList();
        for (int i = 1; i < events.Count; i++)
        {
            var prev = events[i - 1].Id;
            var curr = events[i].Id;
            bool isOrdered = curr.Timestamp > prev.Timestamp ||
                (curr.Timestamp == prev.Timestamp && curr.Sequence > prev.Sequence);
            Assert.True(isOrdered, $"Event ID at index {i} is not ordered");
        }
    }

    [Fact]
    public async Task AppendAsync_should_continue_id_sequence_from_existing_events()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var existingEvent = new Event(new EventId(1000, 5), "lorem", _defaultData);
        var cachedList = new List<Event> { existingEvent };
        var cachedEvents = new CachedEvents(cachedList, new SemaphoreSlim(1, 1));

        var cache = Substitute.For<IStreamsCache>();
        cache.GetEventsAsync(streamId, Arg.Any<CancellationToken>()).Returns(cachedEvents);

        IncomingEventsBatch? capturedBatch = null;
        var channelWriter = Substitute.ForPartsOf<ChannelWriter<IncomingEventsBatch>>();
        channelWriter.TryWrite(Arg.Do<IncomingEventsBatch>(b => capturedBatch = b)).Returns(true);

        var fixedTime = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(fixedTime);
        var idGenerator = new EventIdGenerator(timeProvider);

        var logger = Substitute.For<ILogger<EventsWriter>>();
        var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

        var inputEvents = new[] { new EventData("lorem", new byte[] { 0x42 }) };

        await sut.AppendAsync(streamId, inputEvents);

        Assert.NotNull(capturedBatch);
        var newEvent = capturedBatch.Events.First();
        bool isAfterExisting = newEvent.Id.Timestamp > existingEvent.Id.Timestamp ||
            (newEvent.Id.Timestamp == existingEvent.Id.Timestamp && newEvent.Id.Sequence > existingEvent.Id.Sequence);
        Assert.True(isAfterExisting);
    }
}
