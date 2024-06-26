﻿using EvenireDB.Client.Exceptions;
using EvenireDB.Common;
using EvenireDB.Server.Tests;

namespace EvenireDB.Client.Tests;

public class HttpStreamsClientTests : IClassFixture<ServerFixture>
{
    private const string _defaultStreamsType = "lorem";

    [Fact]
    public async Task GetStreamInfosAsync_should_return_nothing_when_no_streams_available()
    {
        await using var application = new TestServerWebApplicationFactory();

        using var client = application.CreateClient();
        var sut = new HttpStreamsClient(client);
        var results = await sut.GetStreamInfosAsync(_defaultStreamsType);
        results.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetStreamInfosAsync_should_return_ok()
    {
        await using var application = new TestServerWebApplicationFactory();

        using var client = application.CreateClient();
        
        var sut = new HttpStreamsClient(client);
        var results = await sut.GetStreamInfosAsync(_defaultStreamsType);
        results.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStreamInfoAsync_should_throw_when_stream_id_invalid()
    {
        await using var application = new TestServerWebApplicationFactory();

        using var client = application.CreateClient();
        var streamId = new StreamId(Guid.NewGuid(), _defaultStreamsType);
        var sut = new HttpStreamsClient(client);
        await Assert.ThrowsAsync< StreamNotFoundException >(async () => await sut.GetStreamInfoAsync(streamId));
    }

    [Fact]
    public async Task GetStreamInfoAsync_should_return_stream_details_when_existing()
    {
        var streamId = new StreamId(Guid.NewGuid(), _defaultStreamsType);

        await using var application = new TestServerWebApplicationFactory();

        using var client = application.CreateClient();

        var eventsClient = new HttpEventsClient(client);
        await eventsClient.AppendAsync(streamId, TestUtils.BuildEvents(42));

        var sut = new HttpStreamsClient(client);
        var result = await sut.GetStreamInfoAsync(streamId);
        result.Should().NotBeNull();
        result.Id.Should().Be(streamId);
        result.EventsCount.Should().Be(42);
    }

    [Fact]
    public async Task DeleteStreamAsync_should_delete_stream_when_existing()
    {
        var streamId = new StreamId(Guid.NewGuid(), _defaultStreamsType);

        await using var application = new TestServerWebApplicationFactory();

        using var client = application.CreateClient();

        var eventsClient = new HttpEventsClient(client);
        await eventsClient.AppendAsync(streamId, TestUtils.BuildEvents(42));

        var sut = new HttpStreamsClient(client);

        await sut.DeleteStreamAsync(streamId);

        await Assert.ThrowsAsync<StreamNotFoundException>(async () => await sut.GetStreamInfoAsync(streamId));
    }

    [Fact]
    public async Task DeleteStreamAsync_should_throw_when_stream_not_existing()
    {
        var streamId = new StreamId(Guid.NewGuid(), _defaultStreamsType);

        await using var application = new TestServerWebApplicationFactory();

        using var client = application.CreateClient();

        var sut = new HttpStreamsClient(client);

        await Assert.ThrowsAsync<StreamNotFoundException>(async () => await sut.DeleteStreamAsync(streamId));
    }
}
