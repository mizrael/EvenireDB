namespace EvenireDB.Persistence;

public interface IEventsProvider
{
    ValueTask AppendAsync(Guid streamId, string type, IEnumerable<Event> events, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Event> ReadAsync(Guid streamId, string type, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
}