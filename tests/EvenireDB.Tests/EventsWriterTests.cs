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
        result.Should().BeOfType<FailureResult>();

        var failure = (FailureResult)result;
        failure.Code.Should().Be(ErrorCodes.VersionMismatch);
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
        result.Should().BeOfType<FailureResult>();

        var failure = (FailureResult)result;
        failure.Code.Should().Be(ErrorCodes.CannotInitiateWrite);
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
        result.Should().BeOfType<SuccessResult>();
    }
}
