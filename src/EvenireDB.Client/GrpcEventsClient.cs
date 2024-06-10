using EvenireDB.Client.Exceptions;
using EvenireDB.Common;
using Grpc.Core;
using GrpcEvents;
using System.Runtime.CompilerServices;

namespace EvenireDB.Client
{
    internal class GrpcEventsClient : IEventsClient
    {
        private readonly EventsGrpcService.EventsGrpcServiceClient _client;
        
        public GrpcEventsClient(EventsGrpcService.EventsGrpcServiceClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async ValueTask AppendAsync(Guid streamId, IEnumerable<EventData> events, CancellationToken cancellationToken = default)
        {
            var request = new AppendRequest()
            {
                StreamId = streamId.ToString()
            };

            foreach (var @event in events)
            {
                request.Events.Add(new GrpcEvents.EventData()
                {                 
                    Type = @event.Type,
                    Data = Google.Protobuf.UnsafeByteOperations.UnsafeWrap(@event.Data)
                });
            }

            AppendResponse response = null;
            try
            {
                response = await _client.AppendAsync(request, cancellationToken: cancellationToken);                
            }
            catch(Exception ex)
            {
                throw new ClientException(ErrorCodes.Unknown, $"an error has occurred while appending events to stream '{streamId}': {ex.Message}");
            }

            if (response is null)
                throw new ClientException(ErrorCodes.Unknown, "something, somewhere, went terribly, terribly wrong.");

            if (response.Error is not null)
                throw response.Error.Code switch
                {
                    ErrorCodes.DuplicateEvent => new DuplicatedEventException(streamId, response.Error.Message),
                    _ => new ClientException(ErrorCodes.Unknown, response.Error.Message),
                };
        }

        public async IAsyncEnumerable<Event> ReadAsync(
            Guid streamId, 
            StreamPosition position, 
            Direction direction = Direction.Forward, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                var eventId = new EventId(item.Id.Timestamp, (ushort)item.Id.Sequence);

                yield return new Event(eventId, item.Type, item.Data.Memory);
            }
        }
    }
}