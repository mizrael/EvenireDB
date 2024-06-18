using EvenireDB.Common;

namespace EvenireDB
{
    public readonly struct FailureResult : IOperationResult
    {
        public FailureResult(int code, string message)
        {
            this.Code = code;
            this.Message = message;
        }
        
        public string Message { get; } = string.Empty;
        public int Code { get; } = ErrorCodes.Unknown;


        public static FailureResult NullEvents(StreamId streamId)
        => new FailureResult(
                ErrorCodes.BadRequest,
                $"received null events collection for stream {streamId}.");

        public static FailureResult DuplicateEvent(Event? @event)
        => new FailureResult(
                ErrorCodes.DuplicateEvent,
                (@event is null) ? "one of the incoming events is duplicate." :
                $"event '{@event.Id}' is already in the stream.");

        public static IOperationResult CannotInitiateWrite(StreamId streamId)
        => new FailureResult(
                ErrorCodes.CannotInitiateWrite,
                $"unable to write events for stream '{streamId}'.");

        internal static IOperationResult VersionMismatch(StreamId streamId, int expected, int actual)
        => new FailureResult(
                ErrorCodes.VersionMismatch,
                $"stream '{streamId}' is at version {actual} instead of {expected}.");
    }
}