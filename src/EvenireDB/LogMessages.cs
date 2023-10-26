using Microsoft.Extensions.Logging;

namespace EvenireDB
{
    public static partial class LogMessages
    {
        public enum EventIds
        {
            ReadingStreamFromRepository = 0,

            LastId // keep this last
        }

        [LoggerMessage(
            EventId = (int)EventIds.ReadingStreamFromRepository,
            Level = LogLevel.Warning,
            Message = "Reading stream '{StreamId}' from repository")]
        public static partial void ReadingStreamFromRepository(this ILogger logger, Guid streamId);
    }    
}