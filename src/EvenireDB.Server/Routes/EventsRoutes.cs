using EvenireDB.Common;
using EvenireDB.Server.DTO;
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

        private static async IAsyncEnumerable<EventDTO> GetEvents(
            [FromServices] IEventsProvider provider,
            Guid streamId,
            [FromQuery(Name = "pos")] uint startPosition = 0,
            [FromQuery(Name = "dir")] Direction direction = Direction.Forward)
        {
            await foreach (var @event in provider.ReadAsync(streamId, direction: direction, startPosition: startPosition))
                yield return EventDTO.FromModel(@event);
        }

        private static async ValueTask<IResult> SaveEvents(
            [FromServices] EventMapper mapper,
            [FromServices] IEventsProvider provider,
            Guid streamId,
            [FromBody] EventDTO[]? dtos)
        {
            if(dtos is null)
                return Results.BadRequest();

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
                FailureResult { Code: ErrorCodes.DuplicateEvent } d => Results.Conflict(d.Message),
                FailureResult => Results.StatusCode(500),
                _ => Results.AcceptedAtRoute(nameof(GetEvents), new { streamId })
            };
        }
    }    
}