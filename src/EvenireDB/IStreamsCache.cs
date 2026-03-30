using EvenireDB.Common;

namespace EvenireDB;

public interface IStreamsCache
{
    ValueTask<CachedEvents> GetEventsAsync(StreamId streamId, CancellationToken cancellationToken = default);
    bool Contains(StreamId streamId);
    void Remove(StreamId streamId);
}