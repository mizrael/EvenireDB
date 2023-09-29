namespace EvenireDB.Client
{
    public interface IEventsClient
    {
        Task AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);

        Task<IEnumerable<Event>> ReadAsync(Guid streamId, StreamPosition position, Direction direction = Direction.Forward, CancellationToken cancellationToken = default);
    }
}