using EvenireDB.Common;
using Grpc.Core;
using GrpcEvents;

namespace EvenireDB.Server.Grpc
{
    public class EventsService : EventsGrpcService.EventsGrpcServiceBase
    {
        private readonly IEventsProvider _provider;
        private readonly IEventValidator _eventFactory;

        public EventsService(IEventsProvider provider, IEventValidator eventFactory)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _eventFactory = eventFactory ?? throw new ArgumentNullException(nameof(eventFactory));
        }

        public override async Task Read(ReadRequest request, IServerStreamWriter<GrpcEvents.PersistedEvent> responseStream, ServerCallContext context)
        {
            if (!Guid.TryParse(request.StreamId, out var streamId))
                throw new ArgumentOutOfRangeException(nameof(request.StreamId)); //TODO: is this ok?

            await foreach(var @event in _provider.ReadAsync(
                streamId,
                direction: (Direction)request.Direction,
                startPosition: request.StartPosition).ConfigureAwait(false))
            {
                var dto = new GrpcEvents.PersistedEvent()
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
                    _eventFactory.Validate(incoming.Type, incoming.Data.Memory);
                    var @event = new EventData(incoming.Type, incoming.Data.Memory);
                    events.Add(@event);
                }
                
                var result = await _provider.AppendAsync(streamId, events);                

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
