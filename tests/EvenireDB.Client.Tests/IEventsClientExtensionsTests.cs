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

        private GrpcEventsClient CreateSut()
        {
            var channel = _serverFixture.CreateGrpcChannel();
            var client = new GrpcEvents.EventsGrpcService.EventsGrpcServiceClient(channel);
            return new GrpcEventsClient(client);
        }

        [Fact]
        public async Task ReadAllAsync_should_read_entire_stream()
        {
            var streamId = Guid.NewGuid();
            var expectedEvents = TestUtils.BuildEvents(242);

            var sut = CreateSut();
            await sut.AppendAsync(streamId, expectedEvents);

            var receivedEvents = await sut.ReadAllAsync(streamId).ToArrayAsync();
            TestUtils.IsEquivalent(receivedEvents, expectedEvents);
        }
    }
}