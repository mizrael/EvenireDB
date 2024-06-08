namespace EvenireDB;

public readonly record struct StreamInfo(
    Guid StreamId, 
    long EventsCount, 
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessedAt);