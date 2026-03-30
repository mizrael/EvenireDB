using EvenireDB.Server.DTO;
using System.Net.Http.Json;

namespace EvenireDB.Server.Tests.Routes;

public class EventsV1EndpointTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _serverFixture;
    private const string _defaultStreamsType = "lorem";

    public EventsV1EndpointTests(ServerFixture serverFixture)
    {
        _serverFixture = serverFixture;
    }

    [Fact]
    public async Task Get_Archive_should_be_empty_when_no_events_available_for_stream()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/api/v1/streams/{_defaultStreamsType}/{Guid.NewGuid()}/events");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<EventDTO[]>();
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task Post_should_return_bad_request_when_input_null()
    {
        var streamId = Guid.NewGuid();

        await using var application = _serverFixture.CreateServer();
        using var client = application.CreateClient();
        var nullPayloadResponse = await client.PostAsJsonAsync<EventDataDTO[]>($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", null!);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, nullPayloadResponse.StatusCode);
    }

    [Fact]
    public async Task Post_should_return_bad_request_when_input_invalid()
    {
        var streamId = Guid.NewGuid();

        await using var application = _serverFixture.CreateServer();
        using var client = application.CreateClient();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, null);
        var nullDataResponse = await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, nullDataResponse.StatusCode);
    }

    [Fact]
    public async Task Post_should_return_bad_request_when_input_too_big()
    {
        var streamId = Guid.NewGuid();

        await using var application = _serverFixture.CreateServer();
        using var client = application.CreateClient();
        var dtos = HttpRoutesUtils.BuildEventsDTOs(1, new byte[500_001]); //TODO: from config
        var response = await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "TBD")]
    public async Task Post_should_return_conflict_when_input_already_in_stream()
    {
        var streamId = Guid.NewGuid();

        await using var application = _serverFixture.CreateServer();
        using var client = application.CreateClient();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);
        var firstResponse = await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, firstResponse.StatusCode);

        var errorResponse = await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, errorResponse.StatusCode);
    }

    [Fact]
    public async Task Post_should_return_accepted_when_input_valid()
    {
        var streamId = Guid.NewGuid();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);

        await using var application = _serverFixture.CreateServer();
        using var client = application.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Post_should_return_bad_request_when_stream_version_mismatch()
    {
        var streamId = Guid.NewGuid();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);

        await using var application = _serverFixture.CreateServer();
        using var client = application.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);

        var response2 = await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events?version=2", dtos);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task Post_should_create_events()
    {
        var streamId = Guid.NewGuid();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);

        var now = DateTimeOffset.UtcNow;

        await using var application = _serverFixture.CreateServer();
        using var client = application.CreateClient();
        var createResp = await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);
        createResp.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events");
        var fetchedEvents = await response.Content.ReadFromJsonAsync<EventDTO[]>();
        Assert.NotNull(fetchedEvents);
        Assert.Equal(dtos.Length, fetchedEvents.Length);

        for (int i = 0; i != fetchedEvents.Length; i++)
        {
            Assert.NotNull(fetchedEvents[i].Id);
            Assert.Equal(dtos[i].Type, fetchedEvents[i].Type);
            Assert.Equivalent(dtos[i].Data.ToArray(), fetchedEvents[i].Data.ToArray());
        }
    }

}
