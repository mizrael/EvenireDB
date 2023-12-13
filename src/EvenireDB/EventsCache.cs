using EvenireDB.Utils;
using Microsoft.Extensions.Logging;

namespace EvenireDB
{
    internal class EventsCache : IEventsCache
    {
        private readonly ICache<Guid, CachedEvents> _cache;
        private readonly ILogger<EventsCache> _logger;
        private readonly IEventsRepository _repo;

        public EventsCache(
            ILogger<EventsCache> logger,
            ICache<Guid, CachedEvents> cache,
            IEventsRepository repo)
        {
            _logger = logger;
            _cache = cache;
            _repo = repo;
        }

        private async ValueTask<CachedEvents> EventsFactory(Guid streamId, CancellationToken cancellationToken)
        {
            _logger.ReadingStreamFromRepository(streamId);

            var persistedEvents = new List<Event>();
            await foreach (var @event in _repo.ReadAsync(streamId, cancellationToken))
                persistedEvents.Add(@event);
            return new CachedEvents(persistedEvents, new SemaphoreSlim(1));
        }

        public ValueTask<CachedEvents> EnsureStreamAsync(Guid streamId, CancellationToken cancellationToken)
        => _cache.GetOrAddAsync(streamId, this.EventsFactory, cancellationToken);

        public void Update(Guid streamId, CachedEvents entry)
        {
            _cache.AddOrUpdate(streamId, entry);
        }
    }
}