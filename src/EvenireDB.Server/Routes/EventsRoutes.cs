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

        private static async ValueTask<IEnumerable<EventDTO>> GetEvents(
            [FromServices] EventsProvider provider,
            Guid streamId,
            [FromQuery(Name = "skip")] int skip = 0)
        {
            var events = await provider.GetPageAsync(streamId, skip).ConfigureAwait(false);
            if (events is null)
                return Enumerable.Empty<EventDTO>();
            return events.Select(@event => EventDTO.FromModel(@event));
        }

        private static IResult SaveEvents(
           [FromServices] EventsProvider provider,
           Guid streamId,
           [FromBody] EventDTO[] dtos)
        {
            if (dtos is null || dtos.Length == 0)
                return Results.BadRequest();

            Event[] events;

            try
            {
                events = dtos.ToModels();
            }
            catch
            {
                // TODO: build proper response
                return Results.BadRequest();
            }

            provider.AppendAsync(streamId, events);

            return Results.AcceptedAtRoute(nameof(GetEvents), new { streamId });
        }
    }    
}