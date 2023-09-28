namespace EvenireDB
{
    public interface IEventsRepository
    {
        ValueTask<IEnumerable<IEvent>> ReadAsync(Guid streamId, Direction direction = Direction.Forward, long startPosition = (long)StreamPosition.Start, CancellationToken cancellationToken = default);
        ValueTask AppendAsync(Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
    }
}