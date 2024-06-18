using EvenireDB.Server.Tests;

namespace EvenireDB.Client.Tests;

public class IEventsClientExtensionsTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _serverFixture;
    private const string _defaultStreamsType = "lorem";

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
        await sut.AppendAsync(streamId, _defaultStreamsType, expectedEvents);

        var receivedEvents = await sut.ReadAllAsync(streamId, _defaultStreamsType).ToArrayAsync();
        TestUtils.IsEquivalent(receivedEvents, expectedEvents);
    }
}