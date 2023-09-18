using EvenireDB.Server.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

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

        private static async Task<IEnumerable<EventDTO>> GetEvents(
            [FromServices] IEventsRepository repo,
            Guid streamId,
            [FromQuery(Name = "offset")] int offset = 0)
        {
            var events = await repo.ReadAsync(streamId, offset).ConfigureAwait(false);
            if (events is null)
                return Enumerable.Empty<EventDTO>();
            return events.Select(@event => EventDTO.FromModel(@event));
        }

        private static async Task<IResult> SaveEvents(
           [FromServices] ChannelWriter<IncomingEventsGroup> writer,
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
           
            var group = new IncomingEventsGroup(streamId, events);

            await writer.WriteAsync(group)
                        .ConfigureAwait(false);

            return Results.AcceptedAtRoute(nameof(GetEvents), new { streamId });
        }
    }
}