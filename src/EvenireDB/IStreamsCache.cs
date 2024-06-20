using EvenireDB.Common;

namespace EvenireDB;

public interface IStreamsCache
{
    void Update(StreamId streamId, CachedEvents entry);
    ValueTask<CachedEvents> GetEventsAsync(StreamId streamId, CancellationToken cancellationToken = default);
    bool Contains(StreamId streamId);
    void Remove(StreamId streamId);
}