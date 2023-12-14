using EvenireDB.Common;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EvenireDB.Tests
{
    public class EventsWriterTests
    {
        private readonly static byte[] _defaultData = new byte[] { 0x42 };

        [Fact]
        public async Task AppendAsync_should_fail_when_events_duplicated()
        {
            var streamId = Guid.NewGuid();

            var expectedEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            var cache = new LRUCache<Guid, CachedEvents>(1000);
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsWriter(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);
            await sut.AppendAsync(streamId, expectedEvents);
            var result = await sut.AppendAsync(streamId, new[] { expectedEvents[0] });
            result.Should().BeOfType<FailureResult>();

            var failure = (FailureResult)result;
            failure.Code.Should().Be(ErrorCodes.DuplicateEvent);
            failure.Message.Should().Contain(expectedEvents[0].Id.ToString());
        }

        [Fact]
        public async Task AppendAsync_should_fail_when_channel_rejects_message()
        {
            var streamId = Guid.NewGuid();

            var inputEvents = Enumerable.Range(0, 10)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            var cache = new LRUCache<Guid, CachedEvents>(1000);
            var channelWriter = NSubstitute.Substitute.ForPartsOf<ChannelWriter<IncomingEventsGroup>>();
            channelWriter.TryWrite(Arg.Any<IncomingEventsGroup>()).Returns(false);

            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channelWriter, logger);
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
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            var cache = new LRUCache<Guid, CachedEvents>(1000);
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            var result = await sut.AppendAsync(streamId, expectedEvents);
            result.Should().BeOfType<SuccessResult>();
        }
    }
}
