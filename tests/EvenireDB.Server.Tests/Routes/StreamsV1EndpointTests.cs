using EvenireDB.Common;
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
      Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStreamInfo_should_return_ok_when_stream_id_valid()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();

        var streamId = Guid.NewGuid();

        var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);
        await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);

        var response = await client.GetAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}");
     Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var stream = await response.Content.ReadFromJsonAsync<StreamInfo>();
        Assert.NotNull(stream);
  Assert.Equal(streamId, stream.Id.Key);
        Assert.Equal(_defaultStreamsType, stream.Id.Type.ToString());
        Assert.Equal(10, stream.EventsCount);
    }

    [Fact]
    public async Task GetStreamInfo_should_return_not_found_when_stream_id_valid_but_type_invalid()
 {
      await using var application = _serverFixture.CreateServer();

     using var client = application.CreateClient();

    var streamId = Guid.NewGuid();

  var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);
     await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);

   var response = await client.GetAsync($"/api/v1/streams/invalid_type/{streamId}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStreams_should_return_empty_when_no_streams_available()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();
        var response = await client.GetAsync("/api/v1/streams");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var streams = await response.Content.ReadFromJsonAsync<StreamInfo[]>();
 Assert.NotNull(streams);
        Assert.Empty(streams);
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
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var streams = await response.Content.ReadFromJsonAsync<StreamInfo[]>();
        Assert.NotNull(streams);
   Assert.NotEmpty(streams);
        Assert.Single(streams);
        Assert.Contains(streams, s => s.Id.Key == streamId && s.Id.Type == _defaultStreamsType);
    }

    [Fact]
    public async Task GetStreams_should_empty_when_type_invalid()
    {
     await using var application = _serverFixture.CreateServer();

using var client = application.CreateClient();

        var streamId = Guid.NewGuid();

   var dtos = HttpRoutesUtils.BuildEventsDTOs(10, HttpRoutesUtils.DefaultEventData);
    await client.PostAsJsonAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}/events", dtos);

        var response = await client.GetAsync("/api/v1/streams?streamsType=invalid");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

      var streams = await response.Content.ReadFromJsonAsync<StreamInfo[]>();
   Assert.NotNull(streams);
        Assert.Empty(streams);
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
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

   var secondResp = await client.DeleteAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, secondResp.StatusCode);
    }

    [Fact]
    public async Task DeleteSteamAsync_should_return_not_found_when_stream_not_existing()
    {
        await using var application = _serverFixture.CreateServer();

        using var client = application.CreateClient();

   var streamId = Guid.NewGuid();

 var response = await client.DeleteAsync($"/api/v1/streams/{_defaultStreamsType}/{streamId}");
  Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
