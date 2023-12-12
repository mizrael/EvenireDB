using EvenireDB.Common;
using Grpc.Core;
using GrpcEvents;

namespace EvenireDB.Server.Grpc
{
    public class EventsService : EventsGrpcService.EventsGrpcServiceBase
    {
        private readonly IEventsProvider _provider;
        private readonly IEventFactory _eventFactory;

        public EventsService(IEventsProvider provider, IEventFactory eventFactory)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _eventFactory = eventFactory ?? throw new ArgumentNullException(nameof(eventFactory));
        }

        public override async Task Read(ReadRequest request, IServerStreamWriter<GrpcEvents.Event> responseStream, ServerCallContext context)
        {
            if (!Guid.TryParse(request.StreamId, out var streamId))
                throw new ArgumentOutOfRangeException(nameof(request.StreamId)); //TODO: is this ok?

            await foreach(var @event in _provider.ReadAsync(
                streamId,
                direction: (Direction)request.Direction,
                startPosition: request.StartPosition).ConfigureAwait(false))
            {
                var dto = new GrpcEvents.Event()
                {
                    Data = Google.Protobuf.UnsafeByteOperations.UnsafeWrap(@event.Data), 
                    Type = @event.Type,
                    Id = @event.Id.ToString() // TODO: probably bytes would be faster
                };
                await responseStream.WriteAsync(dto).ConfigureAwait(false);
            }
        }

        public override async Task<AppendResponse> Append(AppendRequest request, ServerCallContext context)
        {
            var events = new List<EvenireDB.IEvent>();
            var response = new AppendResponse() { StreamId = request.StreamId };

            try
            {
                foreach (var incoming in request.Events)
                {
                    var eventId = EventId.Parse(incoming.Id);                    
                    var @event = _eventFactory.Create(eventId, incoming.Type, incoming.Data.Memory);
                    events.Add(@event);
                }

                var streamId = Guid.Parse(request.StreamId);
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
