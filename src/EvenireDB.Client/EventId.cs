namespace EvenireDB.Client;

public record EventId(long Timestamp, int Sequence)
{
    public DateTimeOffset CreatedAt => new DateTimeOffset(Timestamp, TimeSpan.Zero);
}