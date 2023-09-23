namespace EvenireDB.Client
{
    public class DuplicatedEventException : ClientException
    {
        public DuplicatedEventException(Guid streamId, string message) : base(ErrorCodes.DuplicateEvent, message)
        {
            StreamId = streamId;
        }

        public Guid StreamId { get; }
    }
}