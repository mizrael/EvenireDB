namespace EvenireDB.Server
{
    public readonly record struct IncomingEventsGroup(Guid AggregateId, Event[] Events);
}