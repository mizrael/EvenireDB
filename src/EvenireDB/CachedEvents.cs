namespace EvenireDB;

public record CachedEvents(List<Event> Events, SemaphoreSlim Semaphore);
