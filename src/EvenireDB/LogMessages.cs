using Microsoft.Extensions.Logging;

namespace EvenireDB;

public static partial class LogMessages
{
    public enum EventIds
    {
        ReadingStreamFromRepository = 0,
        HighMemoryUsageDetected,
        MemoryUsageBelowTreshold,
        EventsGroupPersistenceError,
        AppendingEventsToStream,
        StreamDeletionAttempt,
        StreamDeleted,
        StreamDeletionFailed
    }

    [LoggerMessage(
        EventId = (int)EventIds.ReadingStreamFromRepository,
        Level = LogLevel.Warning,
        Message = "Reading stream '{StreamId}' from repository")]
    public static partial void ReadingStreamFromRepository(this ILogger logger, Guid streamId);

    [LoggerMessage(
        EventId = (int)EventIds.AppendingEventsToStream,
        Level = LogLevel.Debug,
        Message = "Appending {EventsCount} events to stream '{StreamId}'...")]
    public static partial void AppendingEventsToStream(this ILogger logger, int eventsCount, Guid streamId);

    [LoggerMessage(
        EventId = (int)EventIds.HighMemoryUsageDetected,
        Level = LogLevel.Warning,
        Message = "Memory usage is {MemoryUsage} bytes, more than the allowed value: {MaxAllowedAllocatedBytes}. Dropping some cached streams.")]
    public static partial void HighMemoryUsageDetected(this ILogger logger, long memoryUsage, long maxAllowedAllocatedBytes);

    [LoggerMessage(
        EventId = (int)EventIds.MemoryUsageBelowTreshold,
        Level = LogLevel.Debug,
        Message = "Memory usage is {MemoryUsage} / {MaxAllowedAllocatedBytes} bytes")]
    public static partial void MemoryUsageBelowTreshold(this ILogger logger, long memoryUsage, long maxAllowedAllocatedBytes);

    [LoggerMessage(
        EventId = (int)EventIds.EventsGroupPersistenceError,
        Level = LogLevel.Error,
        Message = "an error has occurred while persisting events group for stream {StreamId}: {Error}")]
    public static partial void EventsGroupPersistenceError(this ILogger logger, Guid streamId, string error);

    [LoggerMessage(
        EventId = (int)EventIds.StreamDeletionAttempt,
        Level = LogLevel.Information,
        Message = "Trying to delete stream '{StreamId}' ...")]
    public static partial void StreamDeletionStarted(this ILogger logger, Guid streamId);

    [LoggerMessage(
        EventId = (int)EventIds.StreamDeleted,
        Level = LogLevel.Information,
        Message = "Stream '{StreamId}' deleted.")]
    public static partial void StreamDeleted(this ILogger logger, Guid streamId);

    [LoggerMessage(
        EventId = (int)EventIds.StreamDeletionFailed,
        Level = LogLevel.Critical,
        Message = "Stream '{StreamId}' deletion failed: {Error}")]
    public static partial void StreamDeletionFailed(this ILogger logger, Guid streamId, string error);
}
