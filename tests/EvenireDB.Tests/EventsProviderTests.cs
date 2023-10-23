using EvenireDB.Common;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EvenireDB.Tests
{
    public class EventsProviderTests
    {
        private readonly static byte[] _defaultData = new byte[] { 0x42 };

        [Fact]
        public async Task ReadAsync_should_return_empty_collection_when_data_not_available()
        {
            var repo = Substitute.For<IEventsRepository>();            
            var cache = Substitute.For<ICache<Guid, CachedEvents>>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            var events = await sut.ReadAsync(Guid.NewGuid(), StreamPosition.Start)
                                  .ToListAsync();
            events.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task ReadAsync_should_pull_data_from_repo_on_cache_miss()
        {
            var streamId = Guid.NewGuid();

            List<IEvent> sourceEvents = Enumerable.Range(0, 242)
               .Select(i => (IEvent)new Event(Guid.NewGuid(), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<ICache<Guid, CachedEvents>>();
            cache.GetOrAddAsync(streamId, Arg.Any<Func<Guid, CancellationToken, ValueTask<CachedEvents>>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            var events = await sut.ReadAsync(streamId, StreamPosition.Start)
                                    .ToListAsync();
            events.Should().NotBeNullOrEmpty()
                           .And.HaveCount((int)EventsProviderConfig.Default.MaxPageSize)
                           .And.BeEquivalentTo(sourceEvents.Take((int)EventsProviderConfig.Default.MaxPageSize));
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_backwards()
        {
            var streamId = Guid.NewGuid();

            List<IEvent> sourceEvents = Enumerable.Range(0, 242)
                .Select(i => (IEvent)new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToList();

            var cache = Substitute.For<ICache<Guid, CachedEvents>>();
            cache.GetOrAddAsync(streamId, Arg.Any<Func<Guid, CancellationToken, ValueTask<CachedEvents>>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            var expectedEvents = sourceEvents.Skip(142).ToArray().Reverse();

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: StreamPosition.End, direction: Direction.Backward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount((int)EventsProviderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_backwards_from_position()
        {
            var streamId = Guid.NewGuid();

            List<IEvent> sourceEvents = Enumerable.Range(0, 242)
               .Select(i => (IEvent)new Event(Guid.NewGuid(), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<ICache<Guid, CachedEvents>>();
            cache.GetOrAddAsync(streamId, Arg.Any<Func<Guid, CancellationToken, ValueTask<CachedEvents>>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            var offset = 11;
            StreamPosition startPosition = (uint)(sourceEvents.Count - offset);

            IEnumerable<IEvent> expectedEvents = sourceEvents;
            expectedEvents = expectedEvents.Reverse().Skip(offset-1).Take((int)EventsProviderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: startPosition, direction: Direction.Backward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount((int)EventsProviderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_last_page_backwards_from_position()
        {
            var streamId = Guid.NewGuid();

            List<IEvent> sourceEvents = Enumerable.Range(0, 242)
               .Select(i => (IEvent)new Event(Guid.NewGuid(), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<ICache<Guid, CachedEvents>>();
            cache.GetOrAddAsync(streamId, Arg.Any<Func<Guid, CancellationToken, ValueTask<CachedEvents>>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            var startPosition = EventsProviderConfig.Default.MaxPageSize / 2;

            var expectedEvents = sourceEvents.Take((int)startPosition+1).Reverse();

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: startPosition, direction: Direction.Backward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount(expectedEvents.Count())
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_forward()
        {
            var streamId = Guid.NewGuid();

            List<IEvent> sourceEvents = Enumerable.Range(0, 242)
               .Select(i => (IEvent)new Event(Guid.NewGuid(), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<ICache<Guid, CachedEvents>>();
            cache.GetOrAddAsync(streamId, Arg.Any<Func<Guid, CancellationToken, ValueTask<CachedEvents>>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            var expectedEvents = sourceEvents.Take((int)EventsProviderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: StreamPosition.Start, direction: Direction.Forward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount((int)EventsProviderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_forward_from_position()
        {
            var streamId = Guid.NewGuid();

            List<IEvent> sourceEvents = Enumerable.Range(0, 242)
               .Select(i => (IEvent)new Event(Guid.NewGuid(), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<ICache<Guid, CachedEvents>>();
            cache.GetOrAddAsync(streamId, Arg.Any<Func<Guid, CancellationToken, ValueTask<CachedEvents>>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            StreamPosition startPosition = 11;
            var expectedEvents = sourceEvents.Skip(11).Take((int)EventsProviderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: startPosition, direction: Direction.Forward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount((int)EventsProviderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task AppendAsync_should_fail_when_events_duplicated()
        {
            var streamId = Guid.NewGuid();

            var expectedEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            var cache = new LRUCache<Guid, CachedEvents>(1000);
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);
            await sut.AppendAsync(streamId, expectedEvents);
            var result = await sut.AppendAsync(streamId, new[] { expectedEvents[0] });
            result.Should().BeOfType<FailureResult>();

            var failure = (FailureResult)result;
            failure.Code.Should().Be(ErrorCodes.DuplicateEvent);
            failure.Message.Should().Contain(expectedEvents[0].Id.ToString());
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
            var logger = Substitute.For<ILogger<EventsProvider>>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

            var result = await sut.AppendAsync(streamId, expectedEvents);
            result.Should().BeOfType<SuccessResult>();
        }
    }
}
