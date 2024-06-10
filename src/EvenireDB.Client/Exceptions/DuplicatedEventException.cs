using EvenireDB.Common;

namespace EvenireDB.Client.Exceptions;

public class DuplicatedEventException : ClientException
{
    public DuplicatedEventException(Guid streamId, string message) : base(ErrorCodes.DuplicateEvent, message)
    {
        StreamId = streamId;
    }

    public Guid StreamId { get; }
}
