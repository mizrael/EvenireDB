namespace EvenireDB
{
    public record IncomingEventsGroup(Guid AggregateId, IEnumerable<IEvent> Events);
}