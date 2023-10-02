using Grpc.Core;
using GrpcEvents;

namespace EvenireDB.Server.Grpc
{
    public class EventsService : EventsGrpcService.EventsGrpcServiceBase
    {
        private readonly EventsProvider _provider;

        public EventsService(EventsProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }
        public override async Task Read(ReadRequest request, IServerStreamWriter<EventResponse> responseStream, ServerCallContext context)
        {
            if (!Guid.TryParse(request.StreamId, out var streamId))
                throw new ArgumentOutOfRangeException(nameof(request.StreamId)); //TODO: is this ok?

            await foreach(var @event in _provider.ReadAsync(
                streamId,
                direction: (Direction)request.Direction,
                startPosition: request.StartPosition).ConfigureAwait(false))
            {
                var dto = new EventResponse()
                {
                    Data = Google.Protobuf.ByteString.CopyFrom(@event.Data), //TODO: is there a way to avoid the copy?
                    Type = @event.Type,
                    EventId = @event.Id.ToString() // TODO: probably bytes would be faster
                };
                await responseStream.WriteAsync(dto).ConfigureAwait(false);
            }
                
            await base.Read(request, responseStream, context)
                        .ConfigureAwait(false);
        }
    }
}
