namespace EvenireDB
{
    internal record Event : IEvent
    {
        public Event(EventId id, string type, ReadOnlyMemory<byte> data)
        {
            Id = id;
            Type = type;
            Data = data;
        }

        public EventId Id { get; }
        public string Type { get; }
        public ReadOnlyMemory<byte> Data { get; }
    }
}