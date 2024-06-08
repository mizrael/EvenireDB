namespace EvenireDB.Extents;

public readonly record struct StreamInfo(
    Guid StreamId, 
    long EventsCount, 
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessedAt);