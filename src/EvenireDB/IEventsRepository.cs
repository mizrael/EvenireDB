public interface IEventsRepository
{
    Task<IEnumerable<Event>> ReadAsync(Guid streamId, int skip = 0, CancellationToken cancellationToken = default);
    Task WriteAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
}