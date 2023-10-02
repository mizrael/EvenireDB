namespace EvenireDB.Client
{
    public interface IEventsClient
    {
        ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Event> ReadAsync(Guid streamId, StreamPosition position, Direction direction = Direction.Forward, CancellationToken cancellationToken = default);
    }
}