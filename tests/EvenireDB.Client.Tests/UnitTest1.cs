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
        public async Task GetEventsAsync_should_return_empty_collection_when_no_events_available()
        {
            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            var events = await sut.GetEventsAsync(Guid.NewGuid());
            events.Should().NotBeNull().And.BeEmpty();
        }
    }
}