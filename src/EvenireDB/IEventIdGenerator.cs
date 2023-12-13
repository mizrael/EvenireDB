namespace EvenireDB
{
    public interface IEventIdGenerator
    {
        EventId Generate(EventId? previous = null);
    }
}