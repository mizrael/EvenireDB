namespace EvenireDB
{
    internal record CachedEvents(List<IEvent> Events, SemaphoreSlim Semaphore);
}