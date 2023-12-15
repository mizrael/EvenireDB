namespace EvenireDB
{
    public record EventsReaderConfig(uint MaxPageSize)
    {
        public readonly static EventsReaderConfig Default = new(100);
    }
}