using Microsoft.Extensions.Caching.Memory;
using System.Threading.Channels;

namespace EvenireDB.Server
{
    public record EventsProviderConfig(int MaxPageSize = 100);

    public class EventsProvider
    {
        internal record CachedEvents(List<Event> Events, SemaphoreSlim Semaphore);

        private readonly IMemoryCache _cache;
        private readonly EventsProviderConfig _config;
        private readonly ChannelWriter<IncomingEventsGroup> _writer;
        private readonly static object _lock = new();

        public EventsProvider(IMemoryCache cache, EventsProviderConfig config, ChannelWriter<IncomingEventsGroup> writer)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public IEnumerable<Event> GetPage(Guid streamId, int skip = 0)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            var key = streamId.ToString();
            var entry = _cache.Get<CachedEvents>(key);
            if (entry?.Events == null || entry.Events.Count < skip)
                return Enumerable.Empty<Event>();

            var results = new List<Event>(_config.MaxPageSize);
            var end = Math.Min(skip + _config.MaxPageSize, entry.Events.Count);
            for (var i = skip; i < end; i++)
                results.Add(entry.Events[i]);

            return results;
        }

        public void Append(Guid streamId, IEnumerable<Event> incomingEvents)
        {
            if (incomingEvents is null || !incomingEvents.Any())
                throw new ArgumentNullException(nameof(incomingEvents));

            var key = streamId.ToString();
            CachedEvents entry;

            lock (_lock)
            {
                entry = _cache.GetOrCreate(key, cacheEntry =>
                {
                    cacheEntry.SlidingExpiration = TimeSpan.FromHours(4);
                    return new CachedEvents(new List<Event>(), new SemaphoreSlim(1, 1));
                });
            }

            entry.Semaphore.Wait();

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