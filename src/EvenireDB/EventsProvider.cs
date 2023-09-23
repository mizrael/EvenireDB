using Microsoft.Extensions.Caching.Memory;
using System.Threading.Channels;

namespace EvenireDB
{
    // TODO: logging
    // TODO: append to a transaction log
    // TODO: consider dumping to disk when memory consumption is approaching a threshold (check IMemoryCache.GetCurrentStatistics() )
    public class EventsProvider
    {
        private readonly IMemoryCache _cache;
        private readonly EventsProviderConfig _config;
        private readonly ChannelWriter<IncomingEventsGroup> _writer;
        private readonly IEventsRepository _repo;
        private readonly static SemaphoreSlim _lock = new(1,1);

        internal record CachedEvents(List<Event> Events, SemaphoreSlim Semaphore);

        public EventsProvider(EventsProviderConfig config, IEventsRepository repo, IMemoryCache cache, ChannelWriter<IncomingEventsGroup> writer)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        private async ValueTask<List<Event>> ReadAllPersistedAsync(Guid streamId, CancellationToken cancellationToken = default)
        {
            List<Event> results = new();
            IEnumerable<Event> tmpEvents;
            int skip = 0;
            while (true)
            {
                tmpEvents = await _repo.ReadAsync(streamId, skip, cancellationToken).ConfigureAwait(false);
                if (tmpEvents is null || tmpEvents.Count() == 0) 
                    break;
                results.AddRange(tmpEvents);
                skip += tmpEvents.Count();
            }
            return results;
        }

        private async ValueTask<CachedEvents> EnsureCachedEventsAsync(Guid streamId, string key, CancellationToken cancellationToken)
        {
            var entry = _cache.Get<CachedEvents>(key);
            if (entry is not null)
                return entry;

            _lock.Wait(cancellationToken);
            try
            {
                entry = _cache.Get<CachedEvents>(key);
                if (entry is null)
                {
                    var persistedEvents = await this.ReadAllPersistedAsync(streamId, cancellationToken)
                                                   .ConfigureAwait(false);
                    entry = new CachedEvents(persistedEvents, new SemaphoreSlim(1, 1));

                    _cache.Set(key, entry, _config.CacheDuration);
                }
            }
            finally
            {
                _lock.Release();
            }
            return entry;
        }

        public async ValueTask<IEnumerable<Event>> GetPageAsync(Guid streamId, int skip = 0, CancellationToken cancellationToken = default)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            var key = streamId.ToString();
            
            CachedEvents entry = await EnsureCachedEventsAsync(streamId, key, cancellationToken).ConfigureAwait(false);

            if (entry.Events == null || entry.Events.Count == 0 || entry.Events.Count < skip)
                return Enumerable.Empty<Event>();

            var end = Math.Min(skip + _config.MaxPageSize, entry.Events.Count);
            var count = end - skip;
            var results = new Event[count];
            var j = 0;            
            for (var i = skip; i < end; i++)
                results[j++] = entry.Events[i];
            entry.Events.Except(results);
            return results;
        }

        public async ValueTask<IOperationResult> AppendAsync(Guid streamId, IEnumerable<Event> incomingEvents, CancellationToken cancellationToken = default)
        {
            if (incomingEvents is null)
                throw new ArgumentNullException(nameof(incomingEvents));

            if (!incomingEvents.Any())
                return new SuccessResult();

            var key = streamId.ToString();

            CachedEvents entry = await EnsureCachedEventsAsync(streamId, key, cancellationToken).ConfigureAwait(false);

            entry.Semaphore.Wait(cancellationToken);
            try
            {
                if (entry.Events.Count > 0 &&
                    HasDuplicateEvent(incomingEvents, entry, out var duplicate))
                    return FailureResult.DuplicateEvent(duplicate);

                AddIncomingToCache(incomingEvents, key, entry);

                var group = new IncomingEventsGroup(streamId, incomingEvents);
                _writer.TryWrite(group); //TODO: error checking
            }
            finally
            {
                entry.Semaphore.Release();
            }

            return new SuccessResult();
        }

        private void AddIncomingToCache(IEnumerable<Event> incomingEvents, string key, CachedEvents entry)
        {
            entry.Events.AddRange(incomingEvents);
            _cache.Set(key, entry, _config.CacheDuration);
        }

        private static bool HasDuplicateEvent(IEnumerable<Event> incomingEvents, CachedEvents entry, out Event? duplicate)
        {
            duplicate = null;

            var existingEventIds = new HashSet<Guid>(entry.Events.Count);
            for (int i = 0; i != entry.Events.Count; i++)
                existingEventIds.Add(entry.Events[i].Id);

            foreach (var newEvent in incomingEvents)
            {
                if (existingEventIds.Contains(newEvent.Id))
                {
                    duplicate = newEvent;
                    return true;
                }

                existingEventIds.Add(newEvent.Id);
            }

            return false;
        }
    }
}