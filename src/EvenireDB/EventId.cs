namespace EvenireDB
{
    public readonly struct EventId
    {
        public EventId(ulong timestamp, ushort sequence)
        {
            Timestamp = timestamp;
            Sequence = sequence;
        }

        public ulong Timestamp { get; }
        public ushort Sequence { get; }

        public static EventId Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 3)
                throw new ArgumentException("Invalid event id format", nameof(text));
                
            var parts = text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                throw new ArgumentException("Invalid event id format", nameof(text));

            var timestamp = ulong.Parse(parts[0]);
            var sequence = ushort.Parse(parts[1]);

            return new EventId(timestamp, sequence);
        }
    }
}