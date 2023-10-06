namespace EvenireDB
{
    internal record Event : IEvent
    {
        public Event(Guid id, string type, ReadOnlyMemory<byte> data)
        {
            Id = id;
            Type = type;
            Data = data;
        }

        public Guid Id { get; }
        public string Type { get; }
        public ReadOnlyMemory<byte> Data { get; }
    }
}