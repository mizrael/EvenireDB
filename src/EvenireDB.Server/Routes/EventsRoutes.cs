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
            v1.MapGet("/{aggregateId:guid}", GetEventsByAggregate).WithName(nameof(GetEventsByAggregate));
            v1.MapPost("/{aggregateId:guid}", SaveAggregateEvents).WithName(nameof(SaveAggregateEvents));

            return app;
        }

        private static async Task<IEnumerable<EventDTO>> GetEventsByAggregate(
            [FromServices] IEventsRepository repo,
            Guid aggregateId,
            [FromQuery(Name = "offset")] int offset = 0)
        {
            var events = await repo.ReadAsync(aggregateId, offset).ConfigureAwait(false);
            if (events is null)
                return Enumerable.Empty<EventDTO>();
            return events.Select(@event => EventDTO.FromModel(@event));
        }

        private static async Task<IResult> SaveAggregateEvents(
           [FromServices] ChannelWriter<IncomingEventsGroup> writer,
           Guid aggregateId,
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
           
            var group = new IncomingEventsGroup(aggregateId, events);

            await writer.WriteAsync(group)
                        .ConfigureAwait(false);

            return Results.AcceptedAtRoute(nameof(GetEventsByAggregate), new { aggregateId });
        }
    }
}