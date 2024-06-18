using EvenireDB.Common;

namespace EvenireDB.Persistence;

public interface IEventsProvider
{
    ValueTask AppendAsync(StreamId streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Event> ReadAsync(StreamId streamId, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
}