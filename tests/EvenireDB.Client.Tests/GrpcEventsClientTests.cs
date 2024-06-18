﻿using EvenireDB.Client.Exceptions;
using EvenireDB.Common;
using EvenireDB.Server.Tests;

namespace EvenireDB.Client.Tests;

public class GrpcEventsClientTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _serverFixture;
    private const string _defaultStreamsType = "lorem";

    public GrpcEventsClientTests(ServerFixture serverFixture)
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
    public async Task ReadAsync_should_return_empty_collection_when_no_events_available()
    {
        var sut = CreateSut();

        var events = await sut.ReadAsync(Guid.NewGuid(), _defaultStreamsType).ToListAsync();
        events.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ReadAsync_should_be_able_to_read_backwards()
    {
        var streamId = Guid.NewGuid();
        var inputEvents = TestUtils.BuildEvents(242);

        var sut = CreateSut();

        await sut.AppendAsync(streamId, _defaultStreamsType, inputEvents);

        var expectedEvents = inputEvents.Reverse().Take(100).ToArray();

        var receivedEvents = await sut.ReadAsync(streamId, _defaultStreamsType, position: StreamPosition.End, direction: Direction.Backward).ToArrayAsync();
        receivedEvents.Should().NotBeNullOrEmpty()
                     .And.HaveCount(100);

        TestUtils.IsEquivalent(receivedEvents, expectedEvents);
    }

    [Fact]
    public async Task AppendAsync_should_append_events()
    {
        var streamId = Guid.NewGuid();
        var expectedEvents = TestUtils.BuildEvents(10);

        var sut = CreateSut();

        await sut.AppendAsync(streamId, _defaultStreamsType, expectedEvents);
        var receivedEvents = await sut.ReadAsync(streamId, _defaultStreamsType).ToArrayAsync();

        TestUtils.IsEquivalent(receivedEvents, expectedEvents);
    }

    [Fact(Skip = "TBD")]
    public async Task AppendAsync_should_fail_when_events_already_appended()
    {
        var streamId = Guid.NewGuid();
        var expectedEvents = TestUtils.BuildEvents(10);

        var sut = CreateSut();

        await sut.AppendAsync(streamId, _defaultStreamsType, expectedEvents);
        await Assert.ThrowsAsync<DuplicatedEventException>(async () => await sut.AppendAsync(streamId, _defaultStreamsType, expectedEvents));
    }
}