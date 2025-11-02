using System.Text.Json.Serialization;

namespace EvenireDB.Client;

public readonly struct EventId
{
    [JsonConstructor]
    public EventId(long timestamp, int sequence = 0)
    {
        Timestamp = timestamp;
        Sequence = sequence;
        CreatedAt = new DateTimeOffset(Timestamp, TimeSpan.Zero);
    }

    public EventId(long Timestamp) : this(Timestamp, 0)
    {
    }

    public long Timestamp { get; }
    public int Sequence { get; }
    public DateTimeOffset CreatedAt { get; }
}