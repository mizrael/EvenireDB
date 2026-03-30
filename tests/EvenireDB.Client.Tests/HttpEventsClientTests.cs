using EvenireDB.Client.Exceptions;
using EvenireDB.Common;
using EvenireDB.Server.Tests;

namespace EvenireDB.Client.Tests;

public class HttpEventsClientTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _serverFixture;
    private const string _defaultStreamsType = "lorem";

    public HttpEventsClientTests(ServerFixture serverFixture)
    {
        _serverFixture = serverFixture;
    }

    [Fact]
    public async Task ReadAsync_should_return_empty_collection_when_no_events_available()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();
        var sut = new HttpEventsClient(client);
        var streamId = new StreamId(Guid.NewGuid(), _defaultStreamsType);
        var events = await sut.ReadAsync(streamId).ToListAsync();
      Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task ReadAsync_should_be_able_to_read_backwards()
    {
        var streamId = new StreamId(Guid.NewGuid(), _defaultStreamsType);
   var inputEvents = TestUtils.BuildEventsData(242);

   await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();
      var sut = new HttpEventsClient(client);
    await sut.AppendAsync(streamId, inputEvents);

      var expectedEvents = inputEvents.Reverse().Take(100).ToArray();

        var receivedEvents = await sut.ReadAsync(streamId, position: StreamPosition.End, direction: Direction.Backward).ToArrayAsync();
   TestUtils.IsEquivalent(receivedEvents, expectedEvents);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task AppendAsync_should_append_events(int eventsCount)
    {
        var streamId = new StreamId(_defaultStreamsType);
        var expectedEvents = TestUtils.BuildEvents(eventsCount);

      await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();
        var sut = new HttpEventsClient(client);
        await sut.AppendAsync(streamId, expectedEvents);

        var receivedEvents = await sut.ReadAllAsync(streamId).ToArrayAsync();
        
        TestUtils.IsEquivalent(receivedEvents, expectedEvents);
    }

    [Fact(Skip = "TBD")]
    public async Task AppendAsync_should_fail_when_events_already_appended()
    {
        var streamId = new StreamId(Guid.NewGuid(), _defaultStreamsType);
 var expectedEvents = TestUtils.BuildEvents(10);

   await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();
        var sut = new HttpEventsClient(client);
        await sut.AppendAsync(streamId, expectedEvents);
        await Assert.ThrowsAsync<DuplicatedEventException>(async () => await sut.AppendAsync(streamId, expectedEvents));
    }
}