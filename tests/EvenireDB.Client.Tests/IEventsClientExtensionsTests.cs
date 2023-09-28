using EvenireDB.Server.Tests;

namespace EvenireDB.Client.Tests
{
    public class IEventsClientExtensionsTests : IClassFixture<ServerFixture>
    {
        private readonly ServerFixture _serverFixture;

        public IEventsClientExtensionsTests(ServerFixture serverFixture)
        {
            _serverFixture = serverFixture;
        }

        [Fact]
        public async Task ReadAllAsync_should_read_entire_stream()
        {
            var streamId = Guid.NewGuid();
            var expectedEvents = TestUtils.BuildEvents(242);

            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var sut = new EventsClient(client);
            await sut.AppendAsync(streamId, expectedEvents);

            var receivedEvents = await sut.ReadAllAsync(streamId).ToListAsync();
            receivedEvents.Should().HaveCount(242)
                .And.BeEquivalentTo(expectedEvents);
        }
    }
}