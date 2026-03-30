namespace EvenireDB;

public record EventsReaderConfig(int MaxPageSize)
{
    public readonly static EventsReaderConfig Default = new(100);
}