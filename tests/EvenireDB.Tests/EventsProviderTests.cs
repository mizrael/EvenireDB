using Microsoft.Extensions.Caching.Memory;
using System.Threading.Channels;

namespace EvenireDB.Tests
{
    public class EventsProviderTests
    {
        private readonly static byte[] _defaultData = new byte[] { 0x42 };

        [Fact]
        public async Task GetPageAsync_should_return_empty_collection_when_data_not_available()
        {
            var repo = Substitute.For<IEventsRepository>();
            var cache = Substitute.For<IMemoryCache>();
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var events = await sut.GetPageAsync(Guid.NewGuid());
            events.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task GetPageAsync_should_pull_data_from_repo_on_cache_miss()
        {
            var streamId = Guid.NewGuid();

            var expectedEvents = Enumerable.Range(0, 242)
                .Select(i => new Event(Guid.NewGuid(), "lorem", _defaultData))
                .ToArray();

            var repo = Substitute.For<IEventsRepository>();           
            repo.ReadAsync(streamId, 0, Arg.Any<CancellationToken>())
                .Returns(expectedEvents.Take(100));
            repo.ReadAsync(streamId, 1, Arg.Any<CancellationToken>())
                .Returns(expectedEvents.Skip(100).Take(100));

            var cache = Substitute.For<IMemoryCache>();
            
            var channel = Channel.CreateUnbounded<IncomingEventsGroup>();
            var sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer);

            var events = await sut.GetPageAsync(streamId);
            events.Should().NotBeNullOrEmpty();
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
