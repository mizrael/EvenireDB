using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using System.Threading.Channels;

namespace EvenireDB.Server
{
    public record EventsProviderConfig(TimeSpan CacheDuration, int MaxPageSize)
    {
        public readonly static EventsProviderConfig Default = new(TimeSpan.FromHours(4), 100);
    }

    // TODO: repopulate cache on app restart
    public class EventsProvider
    {
        internal record CachedEvents(List<Event> Events, SemaphoreSlim Semaphore);

        private readonly IMemoryCache _cache;
        private readonly EventsProviderConfig _config;
        private readonly ChannelWriter<IncomingEventsGroup> _writer;
        private readonly IEventsRepository _repo;
        private readonly static SemaphoreSlim _lock = new(1,1);

        public EventsProvider(IMemoryCache cache, EventsProviderConfig config, ChannelWriter<IncomingEventsGroup> writer, IEventsRepository repo)
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

            if (entry.Events == null || entry.Events.Count < skip)
                return Enumerable.Empty<Event>();

            var results = new List<Event>(_config.MaxPageSize);
            var end = Math.Min(skip + _config.MaxPageSize, entry.Events.Count);
            for (var i = skip; i < end; i++)
                results.Add(entry.Events[i]);

            return results;
        }

        public async ValueTask AppendAsync(Guid streamId, IEnumerable<Event> incomingEvents, CancellationToken cancellationToken = default)
        {
            if (incomingEvents is null || !incomingEvents.Any())
                throw new ArgumentNullException(nameof(incomingEvents));

            var key = streamId.ToString();

            CachedEvents entry = await EnsureCachedEventsAsync(streamId, key, cancellationToken).ConfigureAwait(false);

            entry.Semaphore.Wait(cancellationToken);

            try
            {
                if (entry.Events.Count > 0)
                {
                    var existingEventIds = new HashSet<Guid>(entry.Events.Count);
                    for (int i = 0; i != entry.Events.Count; i++)
                        existingEventIds.Add(entry.Events[i].Id);

                    foreach (var newEvent in incomingEvents)
                    {
                        if (entry.Events.Contains(newEvent))
                            throw new ArgumentOutOfRangeException(nameof(newEvent), $"event id '{newEvent.Id}' is duplicated.");
                        existingEventIds.Add(newEvent.Id);
                    }
                }

                entry.Events.AddRange(incomingEvents);

                _cache.Set(key, entry);

                var group = new IncomingEventsGroup(streamId, incomingEvents);
                _writer.TryWrite(group); //TODO: error checking
            }
            finally
            {
                entry.Semaphore.Release();
            }
        }
    }
}