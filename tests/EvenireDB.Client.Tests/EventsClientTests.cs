using EvenireDB.Server.Tests;

namespace EvenireDB.Client.Tests
{
    public class EventsClientTests : IClassFixture<ServerFixture>
    {
        private readonly static byte[] _defaultEventData = new byte[] { 0x42 }; 
        private readonly ServerFixture _serverFixture;

        public EventsClientTests(ServerFixture serverFixture)
        {
            _serverFixture = serverFixture;
        }

        [Fact]
        public async Task GetEventsAsync_should_return_empty_collection_when_no_events_available()
        {
            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            var events = await sut.GetEventsAsync(Guid.NewGuid());
            events.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task AppendAsync_should_append_events()
        {
            var streamId = Guid.NewGuid();
            var expectedEvents = BuildEvents(10);

            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            await sut.AppendAsync(streamId, expectedEvents);
            var receivedEvents = await sut.GetEventsAsync(streamId);
            receivedEvents.Should().BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task AppendAsync_should_fail_when_events_already_appended()
        {
            var streamId = Guid.NewGuid();
            var expectedEvents = BuildEvents(10);

            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            await sut.AppendAsync(streamId, expectedEvents);
            await Assert.ThrowsAsync<DuplicatedEventException>(async () => await sut.AppendAsync(streamId, expectedEvents));
        }

        private Event[] BuildEvents(int count)
            => Enumerable.Range(0, count).Select(i => new Event(Guid.NewGuid(), "lorem", _defaultEventData)).ToArray();
    }
}