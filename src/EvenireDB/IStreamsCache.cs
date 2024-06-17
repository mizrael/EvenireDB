namespace EvenireDB;

public interface IStreamsCache
{
    void Update(Guid streamId, CachedEvents entry);
    ValueTask<CachedEvents> GetEventsAsync(Guid streamId, string streamType, CancellationToken cancellationToken = default);
    bool ContainsKey(Guid streamId);
    void Remove(Guid streamId);
}