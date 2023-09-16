public interface IEventsRepository
{
    Task<IEnumerable<Event>> ReadAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    Task WriteAsync(Guid aggregateId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
}