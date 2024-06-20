using EvenireDB.Common;

namespace EvenireDB;

public interface IEventsReader
{
    IAsyncEnumerable<Event> ReadAsync(
        StreamId streamId,
        StreamPosition startPosition,
        Direction direction = Direction.Forward,
        CancellationToken cancellationToken = default);
}
