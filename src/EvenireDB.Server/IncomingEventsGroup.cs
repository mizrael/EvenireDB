namespace EvenireDB.Server
{
    public record IncomingEventsGroup(Guid AggregateId, IEnumerable<Event> Events);
}