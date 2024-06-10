namespace EvenireDB.Client;

public record StreamInfo(
    Guid StreamId,
    long EventsCount,
    bool IsCached,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessedAt);