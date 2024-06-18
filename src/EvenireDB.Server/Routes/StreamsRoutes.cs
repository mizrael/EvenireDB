﻿using EvenireDB.Common;
using EvenireDB.Server.DTO;
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
        v1.MapGet("/{streamType}/{streamId:guid}", GetStreamInfo).WithName(nameof(GetStreamInfo));
        v1.MapDelete("/{streamType}/{streamId:guid}", DeleteSteamAsync).WithName(nameof(DeleteSteamAsync));
        v1.MapGet("/{streamType}/{streamId:guid}/events", GetEventsAsync).WithName(nameof(GetEventsAsync));
        v1.MapPost("/{streamType}/{streamId:guid}/events", AppendEventsAsync).WithName(nameof(AppendEventsAsync));

        return app;
    }

    private static async ValueTask<IResult> DeleteSteamAsync(
        [FromServices] IStreamInfoProvider provider,
        string streamType,
        Guid streamId)
    {
        try
        {
            await provider.DeleteStreamAsync(streamId, streamType);
            return Results.NoContent();
        }
        catch (ArgumentException)
        {
            return Results.NotFound();
        }
    }

    private static IResult GetStreams(
        [FromServices] IStreamInfoProvider provider,
        [FromQuery] string? streamsType = null) //TODO: test types filter
    {
        var streams = provider.GetStreamsInfo(streamsType);
        return Results.Ok(streams);
    }

    private static IResult GetStreamInfo(
        [FromServices] IStreamInfoProvider provider,
        string streamType,
        Guid streamId)
    {
        var result = provider.GetStreamInfo(streamId, streamType);
        return (result is null) ?
            Results.NotFound() :
            Results.Ok(result);
    }

    private static async IAsyncEnumerable<EventDTO> GetEventsAsync(
        [FromServices] IEventsReader reader,
        string streamType, 
        Guid streamId,
        [FromQuery(Name = "pos")] uint startPosition = 0,
        [FromQuery(Name = "dir")] Direction direction = Direction.Forward)
    {
        await foreach (var @event in reader.ReadAsync(streamId, streamType, direction: direction, startPosition: startPosition).ConfigureAwait(false))
            yield return EventDTO.FromModel(@event);
    }

    private static async ValueTask<IResult> AppendEventsAsync(
        [FromServices] EventMapper mapper,
        [FromServices] IEventsWriter writer,
        string streamType,
        Guid streamId,
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

        var result = await writer.AppendAsync(streamId, streamType, events, expectedVersion)
                                 .ConfigureAwait(false);
        return result switch
        {
            FailureResult { Code: ErrorCodes.DuplicateEvent } d => Results.Conflict(new ApiError(ErrorCodes.DuplicateEvent, d.Message)),
            FailureResult { Code: ErrorCodes.VersionMismatch } d => Results.BadRequest(new ApiError(ErrorCodes.VersionMismatch, d.Message)),
            FailureResult { Code: ErrorCodes.BadRequest } d => Results.BadRequest(new ApiError(ErrorCodes.BadRequest, d.Message)),
            FailureResult => Results.StatusCode(500),
            _ => Results.AcceptedAtRoute(nameof(GetEventsAsync), new { streamId })
        };
    }
}