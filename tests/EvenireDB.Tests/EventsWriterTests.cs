using EvenireDB.Common;
using EvenireDB.Utils;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EvenireDB.Tests
{
    public class EventsWriterTests
    {
        private readonly static byte[] _defaultData = new byte[] { 0x42 };

        [Fact]
        public async Task AppendAsync_should_fail_when_stream_version_mismatch()
        {
            var streamId = Guid.NewGuid();

            var inputEvents = Enumerable.Range(0, 10)
                .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
                .ToArray();

            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>()).Returns(new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1)));

            var channelWriter = NSubstitute.Substitute.ForPartsOf<ChannelWriter<IncomingEventsGroup>>();
            channelWriter.TryWrite(Arg.Any<IncomingEventsGroup>()).Returns(true);

            var idGenerator = Substitute.For<IEventIdGenerator>();
            var logger = Substitute.For<ILogger<EventsWriter>>();
            var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

            await sut.AppendAsync(streamId, inputEvents, version: inputEvents.Count() - 1);
            var result = await sut.AppendAsync(streamId, inputEvents);
            result.Should().BeOfType<FailureResult>();

            var failure = (FailureResult)result;
            failure.Code.Should().Be(ErrorCodes.VersionMismatch);
        }

        [Fact]
        public async Task AppendAsync_should_fail_when_channel_rejects_message()
        {
            var streamId = Guid.NewGuid();

            var inputEvents = Enumerable.Range(0, 10)
                .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
                .ToArray();

            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>()).Returns(new CachedEvents(new List<Event>(), new SemaphoreSlim(1,1)));
           
            var channelWriter = NSubstitute.Substitute.ForPartsOf<ChannelWriter<IncomingEventsGroup>>();
            channelWriter.TryWrite(Arg.Any<IncomingEventsGroup>()).Returns(false);

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
            var streamId = Guid.NewGuid();

            var expectedEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
                .ToArray();            
            
            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>()).Returns(new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1)));

            var channelWriter = NSubstitute.Substitute.ForPartsOf<ChannelWriter<IncomingEventsGroup>>();
            channelWriter.TryWrite(Arg.Any<IncomingEventsGroup>()).Returns(true);

            var idGenerator = Substitute.For<IEventIdGenerator>();
            var logger = Substitute.For<ILogger<EventsWriter>>();
            var sut = new EventsWriter(cache, channelWriter, idGenerator, logger);

            var result = await sut.AppendAsync(streamId, expectedEvents);
            result.Should().BeOfType<SuccessResult>();
        }
    }
}
