using Microsoft.Extensions.Caching.Memory;
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
            var cache = Substitute.For<IMemoryCache>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var events = await sut.ReadAsync(Guid.NewGuid());
            events.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task ReadAsync_should_throw_when_start_position_below_zero()
        {
            var repo = Substitute.For<IEventsRepository>();
            var cache = Substitute.For<IMemoryCache>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await sut.ReadAsync(Guid.NewGuid(), startPosition: -1));
        }

        [Fact]
        public async Task ReadAsync_should_pull_data_from_repo_on_cache_miss()
        {
            var streamId = Guid.NewGuid();

            var expectedEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            repo.ReadAsync(streamId, Arg.Any<CancellationToken>())
                .Returns(expectedEvents.ToAsyncEnumerable());

            var cache = Substitute.For<IMemoryCache>();

            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var events = await sut.ReadAsync(streamId);
            events.Should().NotBeNullOrEmpty()
                           .And.HaveCount(EventsProviderConfig.Default.MaxPageSize)
                           .And.BeEquivalentTo(expectedEvents.Take(EventsProviderConfig.Default.MaxPageSize));
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_backwards()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            repo.ReadAsync(streamId, Arg.Any<CancellationToken>())
                .Returns(sourceEvents.ToAsyncEnumerable());

            var cache = Substitute.For<IMemoryCache>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var expectedEvents = sourceEvents.Skip(142).ToArray().Reverse();

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: (long)StreamPosition.End, direction: Direction.Backward);
            loadedEvents.Should().NotBeNull()
                .And.HaveCount(EventsProviderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_backwards_from_position()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            repo.ReadAsync(streamId, Arg.Any<CancellationToken>())
                .Returns(sourceEvents.ToAsyncEnumerable());

            var cache = Substitute.For<IMemoryCache>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var offset = 11;
            var startPosition = sourceEvents.Length - offset;

            var expectedEvents = sourceEvents.Reverse().Skip(offset-1).Take(EventsProviderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: startPosition, direction: Direction.Backward);
            loadedEvents.Should().NotBeNull()
                .And.HaveCount(EventsProviderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_last_page_backwards_from_position()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            repo.ReadAsync(streamId, Arg.Any<CancellationToken>())
                .Returns(sourceEvents.ToAsyncEnumerable());

            var cache = Substitute.For<IMemoryCache>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var startPosition = EventsProviderConfig.Default.MaxPageSize / 2;

            var expectedEvents = sourceEvents.Take(startPosition+1).Reverse();

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: startPosition, direction: Direction.Backward);
            loadedEvents.Should().NotBeNull()
                .And.HaveCount(expectedEvents.Count())
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_forward()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            repo.ReadAsync(streamId, Arg.Any<CancellationToken>())
                .Returns(sourceEvents.ToAsyncEnumerable());

            var cache = Substitute.For<IMemoryCache>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var expectedEvents = sourceEvents.Take(EventsProviderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: (long)StreamPosition.Start, direction: Direction.Forward);
            loadedEvents.Should().NotBeNull()
                .And.HaveCount(EventsProviderConfig.Default.MaxPageSize)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_forward_from_position()
        {
            var streamId = Guid.NewGuid();

            var sourceEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();
            repo.ReadAsync(streamId, Arg.Any<CancellationToken>())
                .Returns(sourceEvents.ToAsyncEnumerable());

            var cache = Substitute.For<IMemoryCache>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var startPosition = 11;
            var expectedEvents = sourceEvents.Skip(startPosition).Take(EventsProviderConfig.Default.MaxPageSize);

            var loadedEvents = await sut.ReadAsync(streamId, startPosition: startPosition, direction: Direction.Forward);
            loadedEvents.Should().NotBeNull()
                .And.HaveCount(EventsProviderConfig.Default.MaxPageSize)
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
            var cache = new MemoryCache(new MemoryCacheOptions());
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);
            await sut.AppendAsync(streamId, expectedEvents);
            var result = await sut.AppendAsync(streamId, new[] { expectedEvents[0] });
            result.Should().BeOfType<FailureResult>();

            var failure = (FailureResult)result;
            failure.Code.Should().Be(FailureResult.ErrorCodes.DuplicateEvent);
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
            var cache = new MemoryCache(new MemoryCacheOptions());
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var result = await sut.AppendAsync(streamId, expectedEvents);
            result.Should().BeOfType<SuccessResult>();
        }
    }
}
