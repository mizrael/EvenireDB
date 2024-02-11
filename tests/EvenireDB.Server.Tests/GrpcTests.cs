using EvenireDB.Common;
using Google.Protobuf;
using Grpc.Core;
using GrpcEvents;

namespace EvenireDB.Server.Tests
{
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
            };
            var response = client.Read(req);
            var loadedEvents = await response.ResponseStream.ReadAllAsync().ToListAsync();
            loadedEvents.Should().BeEmpty();
        }

        [Fact]
        public async Task Append_should_return_bad_request_when_input_invalid()
        {
            var streamId = Guid.NewGuid();
            var dtos = BuildEventDataDTOs(10, null);

            var channel = _serverFixture.CreateGrpcChannel();
            var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

            var req = new AppendRequest()
            {
                StreamId = streamId.ToString()
            };
            req.Events.AddRange(dtos);
            var response = await client.AppendAsync(req);
            response.Should().NotBeNull();
            response.Error.Should().NotBeNull();
            response.Error.Code.Should().Be(ErrorCodes.BadRequest);
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
                StreamId = streamId.ToString()
            };
            req.Events.AddRange(dtos);
            var response = await client.AppendAsync(req);
            response.Should().NotBeNull();
            response.Error.Should().NotBeNull();
            response.Error.Code.Should().Be(ErrorCodes.BadRequest);
        }

        [Fact]
        public async Task Post_should_return_version_mismatch_when_stream_version_mismatch()
        {
            var streamId = Guid.NewGuid();
            var dtos = BuildEventDataDTOs(1, new byte[500_001]); //TODO: from config

            var channel = _serverFixture.CreateGrpcChannel();
            var client = new EventsGrpcService.EventsGrpcServiceClient(channel);

            var req = new AppendRequest()
            {
                StreamId = streamId.ToString()
            };
            req.Events.AddRange(dtos);
            await client.AppendAsync(req);

            var req2 = new AppendRequest()
            {
                StreamId = streamId.ToString(),
                ExpectedVersion = 71
            };
            req2.Events.AddRange(dtos);
            var response = await client.AppendAsync(req2);
            response.Should().NotBeNull();
            response.Error.Should().NotBeNull();
            response.Error.Code.Should().Be(ErrorCodes.VersionMismatch);
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
                StreamId = streamId.ToString()
            };
            req.Events.AddRange(dtos);
            var response = await client.AppendAsync(req);
            response.Should().NotBeNull();
            response.Error.Should().BeNull();

            var errorResponse = await client.AppendAsync(req);
            errorResponse.Should().NotBeNull();
            errorResponse.Error.Should().NotBeNull();
            errorResponse.Error.Code.Should().Be(ErrorCodes.DuplicateEvent);
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
                StreamId = streamId.ToString()
            };
            req.Events.AddRange(dtos);
            var response = await client.AppendAsync(req);
            response.Should().NotBeNull();
            response.Error.Should().BeNull();

            var readReq = new ReadRequest()
            {
                StreamId = streamId.ToString(),
            };
            var readResponse = client.Read(readReq);
            var loadedEvents = await readResponse.ResponseStream.ReadAllAsync().ToListAsync();
            loadedEvents.Should().HaveCount(dtos.Length);
            foreach(var loadedEvent in loadedEvents)
            {
                loadedEvent.Type.Should().Be("lorem");
                loadedEvent.Data.Memory.ToArray().Should().BeEquivalentTo(_defaultEventData);
            }
        }

        private GrpcEvents.EventData[] BuildEventDataDTOs(int count, byte[]? data)
           => Enumerable.Range(0, count).Select(i => new GrpcEvents.EventData()
           {
               Type = "lorem",
               Data = data is not null && data.Length > 0 ? ByteString.CopyFrom(data) : ByteString.Empty
           }).ToArray();
    }
}