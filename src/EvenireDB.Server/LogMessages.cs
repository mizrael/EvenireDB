namespace EvenireDB.Server
{
    public static partial class LogMessages
    {
        public enum EventIds
        {
            HighMemoryUsageDetected = EvenireDB.LogMessages.EventIds.LastId + 1,
            MemoryUsageBelowTreshold,
            EventsGroupPersistenceError
        }

        [LoggerMessage(
            EventId = (int)EventIds.HighMemoryUsageDetected,
            Level = LogLevel.Warning,
            Message = "Memory usage is {MemoryUsage} bytes, more than the allowed value: {MaxAllowedAllocatedBytes}. Dropping some cached streams")]
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
    }
}