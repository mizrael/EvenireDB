using EvenireDB.Persistence;
using EvenireDB.Utils;
using Microsoft.Extensions.Logging;

namespace EvenireDB;

internal class StreamsCache : IStreamsCache
{
    private readonly ICache<Guid, CachedEvents> _cache;
    private readonly ILogger<StreamsCache> _logger;
    private readonly IEventsProvider _repo;

    public StreamsCache(
        ILogger<StreamsCache> logger,
        ICache<Guid, CachedEvents> cache,
        IEventsProvider repo)
    {
        _logger = logger;
        _cache = cache;
        _repo = repo;
    }

    private async ValueTask<CachedEvents> Factory(Guid streamId, CancellationToken cancellationToken)
    {
        _logger.ReadingStreamFromRepository(streamId);

        var persistedEvents = new List<Event>();
        await foreach (var @event in _repo.ReadAsync(streamId, cancellationToken: cancellationToken))
            persistedEvents.Add(@event);
        return new CachedEvents(persistedEvents, new SemaphoreSlim(1));
    }

    public ValueTask<CachedEvents> GetEventsAsync(Guid streamId, CancellationToken cancellationToken)
    => _cache.GetOrAddAsync(streamId, this.Factory, cancellationToken);

    public void Update(Guid streamId, CachedEvents entry)
    => _cache.AddOrUpdate(streamId, entry);

    public bool ContainsKey(Guid streamId)
    => _cache.ContainsKey(streamId);
}
