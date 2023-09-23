namespace EvenireDB
{
    internal record Event : IEvent
    {
        public Event(Guid id, string type, byte[] data)
        {
            Id = id;
            Type = type;
            Data = data;
        }

        public Guid Id { get; }
        public string Type { get; }
        public byte[] Data { get; }
    }
}