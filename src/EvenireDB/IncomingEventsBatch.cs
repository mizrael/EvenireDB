using EvenireDB.Common;

namespace EvenireDB;

public record IncomingEventsBatch(StreamId StreamId, IEnumerable<Event> Events);