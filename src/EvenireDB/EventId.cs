namespace EvenireDB
{
    public readonly struct EventId
    {
        public EventId(long timestamp, int sequence)
        {
            Timestamp = timestamp;
            Sequence = sequence;
        }

        public long Timestamp { get; }
        public int Sequence { get; }

        public static EventId Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 3)
                throw new ArgumentException("Invalid event id format", nameof(text));
                
            var parts = text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                throw new ArgumentException("Invalid event id format", nameof(text));

            var timestamp = long.Parse(parts[0]);
            var sequence = int.Parse(parts[1]);

            return new EventId(timestamp, sequence);
        }
    }
}