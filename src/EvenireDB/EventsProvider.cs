using EvenireDB.Common;
using EvenireDB.Utils;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EvenireDB
{
    // TODO: logging
    // TODO: append to a transaction log
    internal class EventsProvider : IEventsProvider
    {
        private readonly ICache<Guid, CachedEvents> _cache;
        private readonly EventsProviderConfig _config;
        private readonly ChannelWriter<IncomingEventsGroup> _writer;
        private readonly IEventsRepository _repo;
        private readonly ILogger<EventsProvider> _logger;

        public EventsProvider(
            EventsProviderConfig config,
            IEventsRepository repo,
            ICache<Guid, CachedEvents> cache,
            ChannelWriter<IncomingEventsGroup> writer,
            ILogger<EventsProvider> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async ValueTask<CachedEvents> EventsFactory(Guid streamId, CancellationToken cancellationToken)
        {
            _logger.ReadingStreamFromRepository(streamId);

            var persistedEvents = new List<IEvent>();
            await foreach (var @event in _repo.ReadAsync(streamId, cancellationToken))
                persistedEvents.Add(@event);
            return new CachedEvents(persistedEvents, new SemaphoreSlim(1));
        }

        private ValueTask<CachedEvents> EnsureCachedEventsAsync(Guid streamId, CancellationToken cancellationToken)
        => _cache.GetOrAddAsync(streamId, this.EventsFactory, cancellationToken);

        public async IAsyncEnumerable<IEvent> ReadAsync(
            Guid streamId,
            StreamPosition startPosition,
            Direction direction = Direction.Forward,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (startPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(startPosition));

            CachedEvents entry = await EnsureCachedEventsAsync(streamId, cancellationToken).ConfigureAwait(false);

            if (entry?.Events == null || entry.Events.Count == 0)
                yield break;

            uint totalCount = (uint)entry.Events.Count;
            uint pos = startPosition;

            if (direction == Direction.Forward)
            {
                if (totalCount < startPosition)
                    yield break;

                uint j = 0, i = pos,
                    finalCount = Math.Min(_config.MaxPageSize, totalCount - i);

                while (j++ != finalCount)
                {
                    yield return entry.Events[(int)i++];
                }
            }
            else
            {
                if (startPosition == StreamPosition.End)
                    pos = totalCount - 1;

                if (pos >= totalCount)
                    yield break;

                uint j = 0, i = pos,
                      finalCount = Math.Min(_config.MaxPageSize, i + 1);

                while (j++ != finalCount)
                {
                    yield return entry.Events[(int)i--];
                }
            }
        }

        public async ValueTask<IOperationResult> AppendAsync(Guid streamId, IEnumerable<IEvent> incomingEvents, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(incomingEvents, nameof(incomingEvents));

            if (!incomingEvents.Any())
                return new SuccessResult();

            CachedEvents entry = await EnsureCachedEventsAsync(streamId, cancellationToken).ConfigureAwait(false);

            entry.Semaphore.Wait(cancellationToken);
            try
            {
                if (entry.Events.Count > 0 &&
                    HasDuplicateEvent(incomingEvents, entry, out var duplicate))
                    return FailureResult.DuplicateEvent(duplicate);

                var group = new IncomingEventsGroup(streamId, incomingEvents);
                if (!_writer.TryWrite(group))
                    return FailureResult.CannotInitiateWrite(streamId);

                UpdateCache(streamId, incomingEvents, entry);
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
            _cache.AddOrUpdate(streamId, entry);
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