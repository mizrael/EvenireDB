using Grpc.Core;
using GrpcEventsClient;

namespace EvenireDB.Client
{
    internal class GrpcEventsClient : IEventsClient
    {
        private readonly EventsGrpcService.EventsGrpcServiceClient _client;

        public GrpcEventsClient(EventsGrpcService.EventsGrpcServiceClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<Event> ReadAsync(Guid streamId, StreamPosition position, Direction direction = Direction.Forward, CancellationToken cancellationToken = default)
        {
            var request = new ReadRequest()
            {
                Direction = (uint)direction,
                StartPosition = position,
                StreamId = streamId.ToString(),
            };
            using var response = _client.Read(request, cancellationToken: cancellationToken);
            await foreach(var item in response.ResponseStream.ReadAllAsync().ConfigureAwait(false))
            {
                var eventId = Guid.Parse(item.EventId);
                yield return new Event(eventId, item.Type, item.Data.Memory);
            }
        }
    }
}