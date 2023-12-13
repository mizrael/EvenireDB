namespace EvenireDB
{
    public interface IEventsRepository
    {
        IAsyncEnumerable<Event> ReadAsync(Guid streamId, CancellationToken cancellationToken = default);

        ValueTask AppendAsync(Guid streamId, IEnumerable<EventData> events, CancellationToken cancellationToken = default);
    }
}