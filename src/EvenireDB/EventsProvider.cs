using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Linq;

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

        private async ValueTask<CachedEvents> EnsureCachedEventsAsync(Guid streamId, CancellationToken cancellationToken)
        {
            var key = streamId.ToString();

            var entry = _cache.Get<CachedEvents>(key);
            if (entry is not null)
                return entry;

            _lock.Wait(cancellationToken);
            try
            {
                entry = _cache.Get<CachedEvents>(key);
                if (entry is null)
                {
                    var persistedEvents = new List<IEvent>();
                    await foreach(var @event in _repo.ReadAsync(streamId, cancellationToken))
                        persistedEvents.Add(@event);
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

            CachedEvents entry = await EnsureCachedEventsAsync(streamId, cancellationToken).ConfigureAwait(false);

            if (entry.Events == null || entry.Events.Count == 0)
                return Array.Empty<IEvent>();

            IEvent[] results;

            if (direction == Direction.Forward)
            {
                if (entry.Events.Count < startPosition)
                    return Array.Empty<IEvent>();

                int j = 0, i = (int)startPosition; 
                int count = Math.Min(_config.MaxPageSize, entry.Events.Count - i);
                results = new IEvent[count]; 
                while (j != count)
                {
                    results[j++] = entry.Events[i++];
                }
            }
            else
            {
                if(startPosition == (long)StreamPosition.End)
                    startPosition = entry.Events.Count-1;
               
                if (startPosition >= entry.Events.Count)
                    return Array.Empty<IEvent>();

                int j = 0, i = (int)startPosition;
                int count = Math.Min(_config.MaxPageSize, i + 1);
                results = new IEvent[count];
                while(j != count)
                {
                    results[j++] = entry.Events[i--];
                }
            }

            return results;
        }

        public async ValueTask<IOperationResult> AppendAsync(Guid streamId, IEnumerable<IEvent> incomingEvents, CancellationToken cancellationToken = default)
        {
            if (incomingEvents is null)
                throw new ArgumentNullException(nameof(incomingEvents));

            if (!incomingEvents.Any())
                return new SuccessResult();

            CachedEvents entry = await EnsureCachedEventsAsync(streamId, cancellationToken).ConfigureAwait(false);

            entry.Semaphore.Wait(cancellationToken);
            try
            {
                if (entry.Events.Count > 0 &&
                    HasDuplicateEvent(incomingEvents, entry, out var duplicate))
                    return FailureResult.DuplicateEvent(duplicate);

                UpdateCache(streamId, incomingEvents, entry);

                var group = new IncomingEventsGroup(streamId, incomingEvents);
                _writer.TryWrite(group); //TODO: error checking
            }
            finally
            {
                entry.Semaphore.Release();
            }

            return new SuccessResult();
        }

        private void UpdateCache(Guid streamId, IEnumerable<IEvent> incomingEvents, CachedEvents entry)
        {
            entry.Events.AddRange(incomingEvents);
            _cache.Set(streamId.ToString(), entry, _config.CacheDuration);
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