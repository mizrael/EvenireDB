using Grpc.Core;
using GrpcEvents;

namespace EvenireDB.Server.Grpc
{
    public class EventsService : EventsGrpcService.EventsGrpcServiceBase
    {
        private readonly EventsProvider _provider;
        private readonly IEventFactory _eventFactory;

        public EventsService(EventsProvider provider, IEventFactory eventFactory)
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
                    Data = Google.Protobuf.ByteString.CopyFrom(@event.Data), // TODO: is there a way to avoid the copy?
                    Type = @event.Type,
                    Id = @event.Id.ToString() // TODO: probably bytes would be faster
                };
                await responseStream.WriteAsync(dto).ConfigureAwait(false);
            }
        }

        public override async Task<AppendResponse> Append(AppendRequest request, ServerCallContext context)
        {
            var events = new List<EvenireDB.IEvent>();
            foreach(var incoming in request.Events)
            {
                var eventId = Guid.Parse(incoming.Id);
                var data = incoming.Data.ToArray(); // TODO: is there a way to avoid the copy?
                var @event = _eventFactory.Create(eventId, incoming.Type, data);
                events.Add(@event);
            }

            var streamId = Guid.Parse(request.StreamId);
            var result = await _provider.AppendAsync(streamId, events);

            var response = new AppendResponse() { StreamId = request.StreamId };

            if(result is FailureResult failure)
            {
                response.Error = new FailureResponse()
                {
                    Code = failure.Code,
                    Message = failure.Message,
                };
            }
            
            return response;

        }
    }
}
