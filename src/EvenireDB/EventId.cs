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
    }
}