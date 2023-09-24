namespace EvenireDB
{
    public interface IEventFactory
    {
        IEvent Create(Guid id, string type, byte[] data);
    }
}