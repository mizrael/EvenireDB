namespace EvenireDB;

public record StreamInfo(
    Guid StreamId,
    string StreamType,
    long EventsCount,
    bool IsCached,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessedAt);