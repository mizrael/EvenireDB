public interface IEventsRepository
{
    Task<IEnumerable<Event>> ReadAsync(Guid aggregateId, int offset = 0, CancellationToken cancellationToken = default);
    Task WriteAsync(Guid aggregateId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
}