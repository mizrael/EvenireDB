﻿using EvenireDB.Server.DTO;
using Microsoft.AspNetCore.Mvc;

namespace EvenireDB.Server.Routes
{
    public static class EventsRoutes
    {
        public static WebApplication MapEventsRoutes(this WebApplication app)
        {
            var eventsApi = app.NewVersionedApi();
            var v1 = eventsApi.MapGroup("/api/v{version:apiVersion}/events")
                              .HasApiVersion(1.0);
            v1.MapGet("/{streamId:guid}", GetEvents).WithName(nameof(GetEvents));
            v1.MapPost("/{streamId:guid}", SaveEvents).WithName(nameof(SaveEvents));

            return app;
        }

        private static async ValueTask<IEnumerable<EventDTO>> GetEvents(
            [FromServices] EventsProvider provider,
            Guid streamId,
            [FromQuery(Name = "pos")] uint startPosition = 0,
            [FromQuery(Name = "dir")] Direction direction = Direction.Forward)
        {
            var events = await provider.ReadAsync(streamId, direction: direction, startPosition: startPosition).ConfigureAwait(false);
            return (events is null) ?
                Array.Empty<EventDTO>() :
                events.Select(@event => EventDTO.FromModel(@event));
        }

        private static async ValueTask<IResult> SaveEvents(
            [FromServices] EventMapper mapper,
            [FromServices] EventsProvider provider,
            Guid streamId,
            [FromBody] EventDTO[]? dtos)
        {
            IEvent[] events;

            try
            {
                events = mapper.ToModels(dtos);
            }
            catch
            {
                // TODO: build proper response
                return Results.BadRequest();
            }

            var result = await provider.AppendAsync(streamId, events);
            return result switch
            {
                FailureResult { Code: FailureResult.ErrorCodes.DuplicateEvent } d => Results.Conflict(d.Message),
                FailureResult => Results.StatusCode(500),
                _ => Results.AcceptedAtRoute(nameof(GetEvents), new { streamId })
            };
        }
    }    
}