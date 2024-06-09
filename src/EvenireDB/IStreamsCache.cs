namespace EvenireDB;

public interface IStreamsCache
{
    void Update(Guid streamId, CachedEvents entry);
    ValueTask<CachedEvents> GetEventsAsync(Guid streamId, CancellationToken cancellationToken);
    bool ContainsKey(Guid streamId);
    void Remove(Guid streamId);
}