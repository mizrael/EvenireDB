using EvenireDB.Common;
using EvenireDB.Server.DTO;
using Microsoft.AspNetCore.Mvc;

namespace EvenireDB.Server.Routes
{
    //TODO: add endpoint to get all streams
    public static class StreamsRoutes
    {
        public static WebApplication MapEventsRoutes(this WebApplication app)
        {
            var api = app.NewVersionedApi();
            var v1 = api.MapGroup("/api/v{version:apiVersion}/streams")
                        .HasApiVersion(1.0);
            v1.MapGet("/{streamId:guid}/events", GetEvents).WithName(nameof(GetEvents));
            v1.MapPost("/{streamId:guid}/events", SaveEvents).WithName(nameof(SaveEvents));

            return app;
        }

        private static async IAsyncEnumerable<EventDTO> GetEvents(
            [FromServices] IEventsReader reader,
            Guid streamId,
            [FromQuery(Name = "pos")] uint startPosition = 0,
            [FromQuery(Name = "dir")] Direction direction = Direction.Forward)
        {
            await foreach (var @event in reader.ReadAsync(streamId, direction: direction, startPosition: startPosition))
                yield return EventDTO.FromModel(@event);
        }

        private static async ValueTask<IResult> SaveEvents(
            [FromServices] EventMapper mapper,
            [FromServices] IEventsWriter writer,
            Guid streamId,
            [FromQuery(Name = "version")] int? expectedVersion,
            [FromBody] EventDataDTO[]? dtos)
        {
            if(dtos is null)
                return Results.BadRequest();

            EventData[] events;

            try
            {
                events = mapper.ToModels(dtos);
            }
            catch
            {
                // TODO: build proper response
                return Results.BadRequest();
            }

            var result = await writer.AppendAsync(streamId, events, expectedVersion);
            return result switch
            {
                FailureResult { Code: ErrorCodes.DuplicateEvent } d => Results.Conflict(d.Message),
                FailureResult { Code: ErrorCodes.VersionMismatch } d => Results.BadRequest(d.Message),
                FailureResult => Results.StatusCode(500),
                _ => Results.AcceptedAtRoute(nameof(GetEvents), new { streamId })
            };
        }
    }    
}