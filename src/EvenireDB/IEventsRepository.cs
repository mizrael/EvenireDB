namespace EvenireDB
{
    public interface IEventsRepository
    {
        IAsyncEnumerable<Event> ReadAsync(Guid streamId, CancellationToken cancellationToken = default);

        ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
    }
}