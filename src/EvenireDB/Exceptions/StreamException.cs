using EvenireDB.Common;

namespace EvenireDB.Exceptions;

public class StreamException : Exception
{
    public StreamException(StreamId streamId, string message) : base(message)
    {
        StreamId = streamId;
    }

    public StreamId StreamId { get; }
}