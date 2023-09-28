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

        internal record CachedEvents(List<IEvent> Events, SemaphoreSlim Semaphore);

        public EventsProvider(EventsProviderConfig config, IEventsRepository repo, IMemoryCache cache, ChannelWriter<IncomingEventsGroup> writer)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        private async ValueTask<List<IEvent>> ReadAllPersistedAsync(Guid streamId, CancellationToken cancellationToken = default)
        {
            List<IEvent> results = new();
            IEnumerable<IEvent> tmpEvents;
            long startPosition = 0;
            while (true)
            {
                tmpEvents = await _repo.ReadAsync(streamId, direction: Direction.Forward, startPosition: startPosition, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (tmpEvents is null || tmpEvents.Count() == 0) 
                    break;
                results.AddRange(tmpEvents);
                startPosition += tmpEvents.Count();
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

        public async ValueTask<IEnumerable<IEvent>> ReadAsync(
            Guid streamId,
            Direction direction = Direction.Forward,
            long startPosition = (long)StreamPosition.Start,
            CancellationToken cancellationToken = default)
        {
            if (startPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(startPosition));

            if (direction == Direction.Backward &&
                startPosition != (long)StreamPosition.End)
                throw new ArgumentOutOfRangeException(nameof(startPosition), "When reading backwards, the start position must be set to StreamPosition.End");

            var key = streamId.ToString();
            
            CachedEvents entry = await EnsureCachedEventsAsync(streamId, key, cancellationToken).ConfigureAwait(false);

            if (entry.Events == null || entry.Events.Count == 0 || entry.Events.Count < startPosition)
                return Enumerable.Empty<IEvent>();

            var end = Math.Min(startPosition + _config.MaxPageSize, entry.Events.Count);
            var count = end - startPosition;
            var results = new IEvent[count];
            var j = 0;            
            for (var i = startPosition; i < end; i++)
                results[j++] = entry.Events[(int)i]; // TODO: I don't like this cast here.
            entry.Events.Except(results);
            return results;
        }

        public async ValueTask<IOperationResult> AppendAsync(Guid streamId, IEnumerable<IEvent> incomingEvents, CancellationToken cancellationToken = default)
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

        private void AddIncomingToCache(IEnumerable<IEvent> incomingEvents, string key, CachedEvents entry)
        {
            entry.Events.AddRange(incomingEvents);
            _cache.Set(key, entry, _config.CacheDuration);
        }

        private static bool HasDuplicateEvent(IEnumerable<IEvent> incomingEvents, CachedEvents entry, out IEvent? duplicate)
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