using EvenireDB.Common;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EvenireDB.Tests
{
    public class EventsReaderTests
    {
        private readonly static byte[] _defaultData = new byte[] { 0x42 };

        [Fact]
        public async Task ReadAsync_should_return_empty_collection_when_data_not_available()
        {
            var repo = Substitute.For<IEventsRepository>();            
            var cache = Substitute.For<IEventsCache>();            
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsReader(EventsReaderConfig.Default, repo, cache, logger);

            var events = await sut.ReadAsync(Guid.NewGuid(), StreamPosition.Start)
                                  .ToListAsync();
            events.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task ReadAsync_should_pull_data_from_repo_on_cache_miss()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
               .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();            
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsReader(EventsReaderConfig.Default, repo, cache, logger);

            var events = await sut.ReadAsync(streamId, StreamPosition.Start)
                                    .ToListAsync();
            events.Should().NotBeNullOrEmpty()
                           .And.HaveCount((int)EventsReaderConfig.Default.MaxPageSize)
                           .And.BeEquivalentTo(sourceEvents.Take((int)EventsReaderConfig.Default.MaxPageSize));
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_backwards()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
                .ToList();

            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsReader(EventsReaderConfig.Default, repo, cache, logger);

            var expectedEvents = sourceEvents.Skip(142).ToArray().Reverse();

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: StreamPosition.End, direction: Direction.Backward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount((int)EventsReaderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_backwards_from_position()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
               .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsReader(EventsReaderConfig.Default, repo, cache, logger);

            var offset = 11;
            StreamPosition startPosition = (uint)(sourceEvents.Count - offset);

            IEnumerable<Event> expectedEvents = sourceEvents;
            expectedEvents = expectedEvents.Reverse().Skip(offset-1).Take((int)EventsReaderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: startPosition, direction: Direction.Backward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount((int)EventsReaderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_last_page_backwards_from_position()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
               .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsReader(EventsReaderConfig.Default, repo, cache, logger);

            var startPosition = EventsReaderConfig.Default.MaxPageSize / 2;

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

            var sourceEvents = Enumerable.Range(0, 242)
               .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsReader(EventsReaderConfig.Default, repo, cache, logger);

            var expectedEvents = sourceEvents.Take((int)EventsReaderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: StreamPosition.Start, direction: Direction.Forward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount((int)EventsReaderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_forward_from_position()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
               .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
               .ToList();

            var cache = Substitute.For<IEventsCache>();
            cache.EnsureStreamAsync(streamId, Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<CachedEvents>(new CachedEvents(sourceEvents, new SemaphoreSlim(1))));

            var repo = Substitute.For<IEventsRepository>();
            var logger = Substitute.For<ILogger<EventsReader>>();
            var sut = new EventsReader(EventsReaderConfig.Default, repo, cache, logger);

            StreamPosition startPosition = 11;
            var expectedEvents = sourceEvents.Skip(11).Take((int)EventsReaderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: startPosition, direction: Direction.Forward)
                                        .ToListAsync();
            loadedEvents.Should().NotBeNull()
                .And.HaveCount((int)EventsReaderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }
    }
}
