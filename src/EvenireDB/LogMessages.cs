using Microsoft.Extensions.Logging;

namespace EvenireDB
{
    public static partial class LogMessages
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Warning,
            Message = "Reading stream '{StreamId}' from repository")]
        public static partial void ReadingStreamFromRepository(this ILogger logger, Guid streamId);
    }
}