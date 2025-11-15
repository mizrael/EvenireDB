using EvenireDB.Common;
using Google.Protobuf;
using Grpc.Core;
using GrpcEvents;

namespace EvenireDB.Server.Tests;

public class GrpcTests : IClassFixture<ServerFixture>
{
    private readonly static byte[] _defaultEventData = new byte[] { 0x42 };
    private readonly ServerFixture _serverFixture;

    public GrpcTests(ServerFixture serverFixture)
    {
        _serverFixture = serverFixture;
    }

    [Fact]
    public async Task Get_Archive_should_be_empty_when_no_events_available_for_stream()
    {
        var channel = _serverFixture.CreateGrpcChannel();
        var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

        var req = new ReadRequest()
        {
            StreamId = Guid.NewGuid().ToString(),
            StreamType = "lorem"
        };
        var response = client.Read(req);
        var loadedEvents = await response.ResponseStream.ReadAllAsync().ToArrayAsync();
        Assert.Empty(loadedEvents);
    }

    [Fact]
    public async Task Append_should_return_bad_request_when_events_invalid()
    {
        var streamId = Guid.NewGuid();
        var dtos = BuildEventDataDTOs(10, null);

        var channel = _serverFixture.CreateGrpcChannel();
        var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

        var req = new AppendRequest()
        {
            StreamId = streamId.ToString(),
            StreamType = "lorem"
        };
        req.Events.AddRange(dtos);
        var response = await client.AppendAsync(req);
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.BadRequest, response.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Append_should_return_bad_request_when_stream_type_invalid(string streamType)
    {
        var streamId = Guid.NewGuid();
        var dtos = BuildEventDataDTOs(10, _defaultEventData);

        var channel = _serverFixture.CreateGrpcChannel();
        var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

        var req = new AppendRequest()
        {
            StreamId = streamId.ToString(),
            StreamType = streamType
        };
        req.Events.AddRange(dtos);
        var response = await client.AppendAsync(req);
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.BadRequest, response.Error.Code);
    }

    [Fact]
    public async Task Post_should_return_bad_request_when_input_too_big()
    {
        var streamId = Guid.NewGuid();
        var dtos = BuildEventDataDTOs(1, new byte[500_001]); //TODO: from config

        var channel = _serverFixture.CreateGrpcChannel();
        var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

        var req = new AppendRequest()
        {
            StreamId = streamId.ToString(),
            StreamType = "lorem"
        };
        req.Events.AddRange(dtos);
        var response = await client.AppendAsync(req);
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.BadRequest, response.Error.Code);
    }

    [Fact]
    public async Task Post_should_return_version_mismatch_when_stream_version_mismatch()
    {
        var streamId = Guid.NewGuid();
        var dtos = BuildEventDataDTOs(10, _defaultEventData); 

        var channel = _serverFixture.CreateGrpcChannel();
        var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

        var req = new AppendRequest()
        {
            StreamId = streamId.ToString(),
            StreamType = "lorem"
        };
        req.Events.AddRange(dtos);
        await client.AppendAsync(req);

        var req2 = new AppendRequest()
        {
            StreamId = streamId.ToString(),
            StreamType = "lorem",
            ExpectedVersion = 42
        };
        req2.Events.AddRange(dtos);
        var response = await client.AppendAsync(req2);
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.VersionMismatch, response.Error.Code);
    }

    [Fact(Skip = "TBD")]
    public async Task Post_should_return_conflict_when_input_already_in_stream()
    {
        var streamId = Guid.NewGuid();
        var dtos = BuildEventDataDTOs(10, _defaultEventData);

        var channel = _serverFixture.CreateGrpcChannel();
        var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

        var req = new AppendRequest()
        {
            StreamId = streamId.ToString(),
            StreamType = "lorem"
        };
        req.Events.AddRange(dtos);
        var response = await client.AppendAsync(req);
        Assert.NotNull(response);
        Assert.Null(response.Error);

        var errorResponse = await client.AppendAsync(req);
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(ErrorCodes.DuplicateEvent, errorResponse.Error.Code);
    }

    [Fact]
    public async Task Post_should_succeed_when_input_valid()
    {
        var streamId = Guid.NewGuid();
        var dtos = BuildEventDataDTOs(10, _defaultEventData);

        var channel = _serverFixture.CreateGrpcChannel();
        var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

        var req = new AppendRequest()
        {
            StreamId = streamId.ToString(),
            StreamType = "lorem"
        };
        req.Events.AddRange(dtos);
        var response = await client.AppendAsync(req);
        Assert.NotNull(response);
        Assert.Null(response.Error);

        var readReq = new ReadRequest()
        {
            StreamId = streamId.ToString(),
            StreamType = "lorem"
        };
        var readResponse = client.Read(readReq);
        var loadedEvents = await readResponse.ResponseStream.ReadAllAsync().ToListAsync();
        Assert.Equal(dtos.Length, loadedEvents.Count);
        foreach(var loadedEvent in loadedEvents)
        {
            Assert.Equal("lorem", loadedEvent.Type);
            Assert.Equal(_defaultEventData, loadedEvent.Data.Memory.ToArray());
        }
    }

    private GrpcEvents.EventData[] BuildEventDataDTOs(int count, byte[]? data)
       => Enumerable.Range(0, count).Select(i => new GrpcEvents.EventData()
       {
           Type = "lorem",
           Data = data is not null && data.Length > 0 ? ByteString.CopyFrom(data) : ByteString.Empty
       }).ToArray();
}