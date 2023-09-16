using Microsoft.AspNetCore.Mvc;

public static class EventsRoutes
{
    public static WebApplication MapEventsRoutes(this WebApplication app)
    {
        var eventsApi = app.NewVersionedApi();
        var v1 = eventsApi.MapGroup("/api/v{version:apiVersion}/events")
                          .HasApiVersion(1.0);
        v1.MapGet("/{aggregateId}", GetByAggregate);

        return app;
    }

    private static async Task<IEnumerable<EventDTO>> GetByAggregate([FromServices] IEventsRepository repo, Guid aggregateId)
    { 
        var events = await repo.ReadAsync(aggregateId).ConfigureAwait(false);
        if(events is null) 
            return Enumerable.Empty<EventDTO>();
        return events.Select(@event => EventDTO.FromModel(@event));        
    }
}