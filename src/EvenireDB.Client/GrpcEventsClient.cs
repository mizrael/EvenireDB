﻿using Grpc.Core;
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

        public async ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
        {
            var request = new AppendRequest()
            {
                StreamId = streamId.ToString()
            };
            foreach (var @event in events)
            {
                request.Events.Add(new GrpcEvents.Event()
                {
                    Id = @event.Id.ToString(),
                    Type = @event.Type,
                    Data = Google.Protobuf.ByteString.CopyFrom(@event.Data) //TODO: is there a way to avoid the copy?
                });
            }

            var response = await _client.AppendAsync(request, cancellationToken: cancellationToken);
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
                var eventId = Guid.Parse(item.Id);
                yield return new Event(eventId, item.Type, item.Data.ToArray()); //TODO: is there a way to avoid the copy?
            }
        }
    }
}