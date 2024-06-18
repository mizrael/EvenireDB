using EvenireDB.Common;
using EvenireDB.Persistence;
using EvenireDB.Utils;
using Microsoft.Extensions.Logging;

namespace EvenireDB;

internal class StreamsCache : IStreamsCache
{
    private readonly ICache<StreamId, CachedEvents> _cache;
    private readonly ILogger<StreamsCache> _logger;
    private readonly IEventsProvider _repo;

    public StreamsCache(
        ILogger<StreamsCache> logger,
        ICache<StreamId, CachedEvents> cache,
        IEventsProvider repo)
    {
        _logger = logger;
        _cache = cache;
        _repo = repo;
    }

    private async ValueTask<CachedEvents> Factory(StreamId streamId, CancellationToken cancellationToken)
    {
        _logger.ReadingStreamFromRepository(streamId);

        var persistedEvents = new List<Event>();
        await foreach (var @event in _repo.ReadAsync(streamId, cancellationToken: cancellationToken))
            persistedEvents.Add(@event);
        return new CachedEvents(persistedEvents, new SemaphoreSlim(1));
    }

    public ValueTask<CachedEvents> GetEventsAsync(StreamId streamId, CancellationToken cancellationToken = default)
    => _cache.GetOrAddAsync(streamId, (_,_) => this.Factory(streamId, cancellationToken), cancellationToken);

    public void Update(StreamId streamId, CachedEvents entry)
    => _cache.AddOrUpdate(streamId, entry);

    public bool Contains(StreamId streamId)
    => _cache.ContainsKey(streamId);

    public void Remove(StreamId streamId)
    => _cache.Remove(streamId);
}
