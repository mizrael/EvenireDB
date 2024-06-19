using EvenireDB.Common;

namespace EvenireDB.Client.Exceptions;

public class DuplicatedEventException : ClientException
{
    public DuplicatedEventException(StreamId streamId, string message) : base(ErrorCodes.DuplicateEvent, message)
    {
        StreamId = streamId;
    }

    public StreamId StreamId { get; }
}
