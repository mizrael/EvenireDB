namespace EvenireDB
{
    public interface IEventsRepository
    {
        IAsyncEnumerable<IEvent> ReadAsync(Guid streamId, CancellationToken cancellationToken = default);

        ValueTask AppendAsync(Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
    }
}