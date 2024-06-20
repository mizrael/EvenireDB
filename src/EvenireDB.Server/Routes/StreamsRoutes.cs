using EvenireDB.Common;
using EvenireDB.Server.DTO;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;

namespace EvenireDB.Server.Routes;

public static class StreamsRoutes
{
    public static WebApplication MapEventsRoutes(this WebApplication app)
    {
        var api = app.NewVersionedApi();
        var v1 = api.MapGroup("/api/v{version:apiVersion}/streams")
                    .HasApiVersion(1.0);
        v1.MapGet("", GetStreams).WithName(nameof(GetStreams));
        v1.MapGet("/{streamType}/{streamKey:guid}", GetStreamInfo).WithName(nameof(GetStreamInfo));
        v1.MapDelete("/{streamType}/{streamKey:guid}", DeleteSteamAsync).WithName(nameof(DeleteSteamAsync));
        v1.MapGet("/{streamType}/{streamKey:guid}/events", GetEventsAsync).WithName(nameof(GetEventsAsync));
        v1.MapPost("/{streamType}/{streamKey:guid}/events", AppendEventsAsync).WithName(nameof(AppendEventsAsync));

        return app;
    }

    private static async ValueTask<IResult> DeleteSteamAsync(
        [FromServices] IStreamInfoProvider provider,
        string streamType,
        Guid streamKey)
    {
        try
        {
            var streamId = new StreamId { Key = streamKey, Type = streamType };
            var result = await provider.DeleteStreamAsync(streamId);
            return result ? Results.NoContent() : Results.NotFound();
        }
        catch (ArgumentException)
        {
            return Results.BadRequest();
        }
    }

    private static IResult GetStreams(
        [FromServices] IStreamInfoProvider provider,
        [FromQuery] StreamType? streamsType = null) //TODO: test types filter
    {
        var streams = provider.GetStreamsInfo(streamsType);
        return Results.Ok(streams);
    }

    private static IResult GetStreamInfo(
        [FromServices] IStreamInfoProvider provider,
        string streamType,
        Guid streamKey)
    {
        try
        {
            var streamId = new StreamId { Key = streamKey, Type = streamType };
            var result = provider.GetStreamInfo(streamId);
            return (result is null) ?
                Results.NotFound() :
                Results.Ok(result);
        }
        catch (ArgumentException)
        {
            return Results.BadRequest();
        }       
    }

    private static async IAsyncEnumerable<EventDTO> GetEventsAsync(
        [FromServices] IEventsReader reader,
        string streamType, 
        Guid streamKey,
        [FromQuery(Name = "pos")] uint startPosition = 0,
        [FromQuery(Name = "dir")] Direction direction = Direction.Forward)
    {
        //TODO: handle malformed stream id

        var streamId = new StreamId { Key = streamKey, Type = streamType };
        await foreach (var @event in reader.ReadAsync(streamId, direction: direction, startPosition: startPosition).ConfigureAwait(false))
            yield return EventDTO.FromModel(@event);
    }

    private static async ValueTask<IResult> AppendEventsAsync(
        [FromServices] EventMapper mapper,
        [FromServices] IEventsWriter writer,
        string streamType,
        Guid streamKey,
        [FromQuery(Name = "version")] int? expectedVersion,
        [FromBody] EventDataDTO[]? dtos)
    {
        if (dtos is null)
            return Results.BadRequest(new ApiError(ErrorCodes.BadRequest, "No events provided"));

        EventData[] events;

        try
        {
            events = mapper.ToModels(dtos);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ApiError(ErrorCodes.BadRequest, ex.Message));
        }

        //TODO: handle malformed stream id

        var streamId = new StreamId { Key = streamKey, Type = streamType };
        var result = await writer.AppendAsync(streamId, events, expectedVersion)
                                 .ConfigureAwait(false);
        return result switch
        {
            FailureResult { Code: ErrorCodes.DuplicateEvent } d => Results.Conflict(new ApiError(ErrorCodes.DuplicateEvent, d.Message)),
            FailureResult { Code: ErrorCodes.VersionMismatch } d => Results.BadRequest(new ApiError(ErrorCodes.VersionMismatch, d.Message)),
            FailureResult { Code: ErrorCodes.BadRequest } d => Results.BadRequest(new ApiError(ErrorCodes.BadRequest, d.Message)),
            FailureResult => Results.StatusCode(500),
            _ => Results.AcceptedAtRoute(nameof(GetEventsAsync), new { streamKey })
        };
    }
}