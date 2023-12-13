namespace EvenireDB.Client
{
    public record PersistedEvent : Event
    {
        public PersistedEvent(EventId id, string type, ReadOnlyMemory<byte> data) : base(type, data)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public EventId Id { get; }
    }
}