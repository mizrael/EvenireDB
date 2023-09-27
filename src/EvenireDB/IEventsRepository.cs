namespace EvenireDB
{
    public interface IEventsRepository
    {
        ValueTask<IEnumerable<IEvent>> ReadAsync(Guid streamId, Direction direction = Direction.Forward, int skip = 0, CancellationToken cancellationToken = default);
        ValueTask WriteAsync(Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
    }
}