namespace EvenireDB.Persistence;

public interface IEventsProvider
{
    ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Event> ReadAsync(Guid streamId, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
}