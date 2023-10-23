namespace EvenireDB
{
    public record EventsProviderConfig(uint MaxPageSize)
    {
        public readonly static EventsProviderConfig Default = new(100);
    }
}