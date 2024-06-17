namespace EvenireDB;

public record IncomingEventsBatch(Guid StreamId, string StreamType, IEnumerable<Event> Events);