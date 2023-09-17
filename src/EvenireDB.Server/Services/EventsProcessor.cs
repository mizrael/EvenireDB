using EvenireDB.Server.DTO;
using System.Collections.Concurrent;

namespace EvenireDB.Server.Services
{
    public class EventsProcessor : IEventsProcessor
    {
        private readonly record struct EventsGroup(Guid AggregateId, EventDTO[] Events);

        private readonly EventsProcessorConfig _config;
        private readonly IEventsRepository _repo;
        private readonly ConcurrentQueue<EventsGroup> _events = new();
        private DateTimeOffset _lastUpdate;

        public EventsProcessor(EventsProcessorConfig config, IEventsRepository repo)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public void Enqueue(Guid aggregateId, EventDTO[] events)
            => _events.Enqueue(new EventsGroup(aggregateId, events));

        public async Task PersistPendingAsync(CancellationToken cancellationToken = default)
        {
            var deltaTime = DateTimeOffset.UtcNow - _lastUpdate;
            var canProcess = deltaTime >= _config.FlushTimeout || _events.Count >= _config.MaxGroupsCount;
            if (!canProcess)
                return;

            _lastUpdate = DateTimeOffset.UtcNow;

            while (_events.TryDequeue(out EventsGroup group))
            {
                await _repo.WriteAsync(group.AggregateId, group.Events.ToModels(), cancellationToken)
                           .ConfigureAwait(false);
            }
        }
    }
}