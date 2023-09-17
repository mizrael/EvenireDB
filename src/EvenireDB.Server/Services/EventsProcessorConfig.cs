namespace EvenireDB.Server.Services
{
    public record EventsProcessorConfig(int MaxGroupsCount, TimeSpan FlushTimeout)
    {
        public readonly static EventsProcessorConfig Default = new(10, TimeSpan.FromSeconds(5));
    }
}