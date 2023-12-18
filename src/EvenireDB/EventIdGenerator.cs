namespace EvenireDB
{
    internal class EventIdGenerator : IEventIdGenerator
    {
        private readonly TimeProvider _timeProvider;

        public EventIdGenerator(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        //TODO: tests
        public EventId Generate(EventId? previous = null)
        {
            int sequence = previous?.Sequence ?? 0;
            var ticks = _timeProvider.GetUtcNow().UtcTicks;
            if (previous.HasValue && previous.Value.Timestamp >= ticks)
                sequence++;
            return new EventId(ticks, sequence);
        }
    }
}