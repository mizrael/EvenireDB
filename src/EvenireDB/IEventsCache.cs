namespace EvenireDB
{
    public interface IEventsCache {
        void Update(Guid streamId, CachedEvents entry);
        ValueTask<CachedEvents> EnsureStreamAsync(Guid streamId, CancellationToken cancellationToken);
    }
}