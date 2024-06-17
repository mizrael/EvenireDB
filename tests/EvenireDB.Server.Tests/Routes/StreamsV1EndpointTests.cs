using System.Net.Http.Json;

namespace EvenireDB.Server.Tests.Routes;

public class StreamsV1EndpointTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _serverFixture;
    private const string _defaultStreamsType = "lorem";

    public StreamsV1EndpointTests(ServerFixture serverFixture)
    {
        _serverFixture = serverFixture;
    }

    [Fact]
    public async Task GetStreams_should_return_not_found_when_stream_id_invalid()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/api/v1/streams/{_defaultStreamsType}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStreams_should_return_ok_when_stream_id_valid()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();

        var streamId = Guid.NewGuid();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);
        await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);

        var response = await client.GetAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var stream = await response.Content.ReadFromJsonAsync<StreamInfo>();
        stream.Should().NotBeNull();
        stream.StreamId.Should().Be(streamId);
        stream.EventsCount.Should().Be(10);
    }

    [Fact]
    public async Task GetStreams_should_return_empty_when_no_streams_available()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();
        var response = await client.GetAsync("/api/v1/streams");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var streams = await response.Content.ReadFromJsonAsync<StreamInfo[]>();
        streams.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetStreams_should_return_available_streams()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();

        var streamId = Guid.NewGuid();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);
        await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);

        var response = await client.GetAsync("/api/v1/streams");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var streams = await response.Content.ReadFromJsonAsync<StreamInfo[]>();
        streams.Should().NotBeNull()
                    .And.NotBeEmpty()
                    .And.HaveCount(1)
                    .And.Contain(s => s.StreamId == streamId);
    }

    [Fact]
    public async Task DeleteSteamAsync_should_delete_existing_stream()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();

        var streamId = Guid.NewGuid();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);
        await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);

        var response = await client.DeleteAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var secondResp = await client.DeleteAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}");
        secondResp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSteamAsync_should_return_not_found_when_stream_not_existing()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();

        var streamId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
