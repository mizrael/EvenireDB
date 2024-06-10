using EvenireDB.Common;

namespace EvenireDB.Client.Exceptions;

public class StreamNotFoundException : ClientException
{
    public StreamNotFoundException(Guid streamId) : base(ErrorCodes.BadRequest, $"stream '{streamId}' does not exist.")
    {
        StreamId = streamId;
    }

    public Guid StreamId { get; }
}