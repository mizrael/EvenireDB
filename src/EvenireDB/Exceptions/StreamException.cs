namespace EvenireDB.Exceptions;

public class StreamException : Exception
{
    public StreamException(Guid streamId, string message) : base(message)
    {
        StreamId = streamId;
    }

    public Guid StreamId { get; }
}