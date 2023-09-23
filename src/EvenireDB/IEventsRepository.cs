namespace EvenireDB
{
    public interface IEventsRepository
    {
        ValueTask<IEnumerable<IEvent>> ReadAsync(Guid streamId, int skip = 0, CancellationToken cancellationToken = default);
        ValueTask WriteAsync(Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
    }
}