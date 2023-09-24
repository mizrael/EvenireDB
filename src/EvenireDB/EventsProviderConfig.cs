namespace EvenireDB
{
    public record EventsProviderConfig(
        TimeSpan CacheDuration, 
        int MaxPageSize);
    {
        public readonly static EventsProviderConfig Default = new(TimeSpan.FromHours(4), 100);
    }
}