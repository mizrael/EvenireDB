namespace EvenireDB
{
    public record IncomingEventsGroup(Guid AggregateId, IEnumerable<Event> Events);
}