namespace EvenireDB
{
    public interface IEventsRepository
    {
        ValueTask<IEnumerable<Event>> ReadPageAsync(Guid streamId, int streamPosition, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Event> ReadAsync(Guid streamId, CancellationToken cancellationToken = default);

        ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
    }
}