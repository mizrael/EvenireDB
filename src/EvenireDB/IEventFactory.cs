namespace EvenireDB
{
    public interface IEventFactory
    {
        IEvent Create(EventId id, string type, ReadOnlyMemory<byte> data);
    }
}