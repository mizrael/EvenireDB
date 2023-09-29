using EvenireDB.Server.Tests;

namespace EvenireDB.Client.Tests
{
    public class EventsClientTests : IClassFixture<ServerFixture>
    {
        private readonly ServerFixture _serverFixture;

        public EventsClientTests(ServerFixture serverFixture)
        {
            _serverFixture = serverFixture;
        }

        [Fact]
        public async Task ReadAsync_should_return_empty_collection_when_no_events_available()
        {
            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            var events = await sut.ReadAsync(Guid.NewGuid());
            events.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task ReadAsync_should_be_able_to_read_backwards()
        {
            var streamId = Guid.NewGuid();
            var inputEvents = TestUtils.BuildEvents(242);

            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            await sut.AppendAsync(streamId, inputEvents);

            var expectedEvents = inputEvents.Reverse().Take(100);

            var receivedEvents = await sut.ReadAsync(streamId, position: StreamPosition.End, direction: Direction.Backward);
            receivedEvents.Should().HaveCount(100)
                .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task AppendAsync_should_append_events()
        {
            var streamId = Guid.NewGuid();
            var expectedEvents = TestUtils.BuildEvents(10);

            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            await sut.AppendAsync(streamId, expectedEvents);
            var receivedEvents = await sut.ReadAsync(streamId);
            receivedEvents.Should().BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task AppendAsync_should_fail_when_events_already_appended()
        {
            var streamId = Guid.NewGuid();
            var expectedEvents = TestUtils.BuildEvents(10);

            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            await sut.AppendAsync(streamId, expectedEvents);
            await Assert.ThrowsAsync<DuplicatedEventException>(async () => await sut.AppendAsync(streamId, expectedEvents));
        }
    }
}