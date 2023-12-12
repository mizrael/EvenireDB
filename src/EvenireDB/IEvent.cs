// TODO: add creation date
// TODO: consider using a string instead of a guid for the stream id
namespace EvenireDB
{
    public interface IEvent
    {
        EventId Id { get; } // TODO: consider using a timestamp instead, like Redis streams
        string Type { get; }
        ReadOnlyMemory<byte> Data { get; }
    }
}