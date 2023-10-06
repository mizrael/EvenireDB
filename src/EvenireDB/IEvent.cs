// TODO: add creation date
namespace EvenireDB
{
    public interface IEvent
    {
        Guid Id { get; }
        string Type { get; }
        ReadOnlyMemory<byte> Data { get; }
    }
}