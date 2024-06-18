namespace EvenireDB.Common;

public record StreamInfo(
    StreamId Id,
    long EventsCount,
    bool IsCached,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessedAt);
