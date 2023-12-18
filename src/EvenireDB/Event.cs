namespace EvenireDB
{
    public record Event : EventData
    {
        public Event(EventId id, string type, ReadOnlyMemory<byte> data) : base(type, data)
        {
            this.Id = id;
        }

        public EventId Id { get; }
    }
}