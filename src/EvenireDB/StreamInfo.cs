namespace EvenireDB;

public readonly record struct StreamInfo(
    Guid StreamId, 
    long EventsCount, 
    bool IsCached,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessedAt);