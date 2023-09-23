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

        public static FailureResult DuplicateEvent(IEvent? @event)
            => new FailureResult(
                ErrorCodes.DuplicateEvent,
                (@event is null) ? "one of the incoming events is duplicate." :
                $"event '{@event.Id}' is already in the stream.");

        public sealed class ErrorCodes
        {
            public const int Unknown = -1;
            public const int DuplicateEvent = -2;
        }
    }
}