using EvenireDB.Common;

namespace EvenireDB.Client;

public interface IEventsClient
{
    //TODO: add response
    ValueTask AppendAsync(StreamId streamId, IEnumerable<EventData> events, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Event> ReadAsync(StreamId streamId, StreamPosition position, Direction direction = Direction.Forward, CancellationToken cancellationToken = default);
}
