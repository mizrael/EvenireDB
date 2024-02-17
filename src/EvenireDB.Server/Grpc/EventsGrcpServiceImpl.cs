using EvenireDB.Common;
using Grpc.Core;
using GrpcEvents;

namespace EvenireDB.Server.Grpc
{
    public class EventsGrcpServiceImpl : EventsGrpcService.EventsGrpcServiceBase
    {
        private readonly IEventsReader _reader;
        private readonly IEventsWriter _writer;
        private readonly IEventDataValidator _validator;

        public EventsGrcpServiceImpl(IEventsReader reader, IEventDataValidator validator, IEventsWriter writer)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public override async Task Read(ReadRequest request, IServerStreamWriter<GrpcEvents.Event> responseStream, ServerCallContext context)
        {
            if (!Guid.TryParse(request.StreamId, out var streamId))
                throw new ArgumentOutOfRangeException(nameof(request.StreamId)); //TODO: is this ok?

            await foreach(var @event in _reader.ReadAsync(
                streamId,
                direction: (Direction)request.Direction,
                startPosition: request.StartPosition).ConfigureAwait(false))
            {
                var dto = new GrpcEvents.Event()
                {
                    Data = Google.Protobuf.UnsafeByteOperations.UnsafeWrap(@event.Data), 
                    Type = @event.Type,
                    Id = new GrpcEvents.EventId()
                    {
                        Timestamp = @event.Id.Timestamp,
                        Sequence = @event.Id.Sequence
                    }
                };
                await responseStream.WriteAsync(dto).ConfigureAwait(false);
            }
        }

        public override async Task<AppendResponse> Append(AppendRequest request, ServerCallContext context)
        {
            var events = new List<EventData>(request.Events.Count);
            var response = new AppendResponse() { StreamId = request.StreamId };

            try
            {
                var streamId = Guid.Parse(request.StreamId);

                foreach (var incoming in request.Events)
                {
                    _validator.Validate(incoming.Type, incoming.Data.Memory);
                    var @event = new EventData(incoming.Type, incoming.Data.Memory);
                    events.Add(@event);
                }
                
                var result = await _writer.AppendAsync(streamId, events, request.ExpectedVersion)
                                          .ConfigureAwait(false);                

                if (result is FailureResult failure)
                {
                    response.Error = new FailureResponse()
                    {
                        Code = failure.Code,
                        Message = failure.Message,
                    };
                }
            }
            catch(ArgumentException ex)
            {
                response.Error = new FailureResponse() { Code = ErrorCodes.BadRequest, Message = ex.Message };
            }
            catch (Exception ex)
            {
                response.Error = new FailureResponse() { Code = ErrorCodes.Unknown, Message = ex.Message };
            }

            return response;
        }
    }
}
