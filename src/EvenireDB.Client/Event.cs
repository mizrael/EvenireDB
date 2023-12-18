namespace EvenireDB.Client
{
    public record Event : EventData
    {
        public Event(EventId id, string type, ReadOnlyMemory<byte> data) : base(type, data)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public EventId Id { get; }
    }
}