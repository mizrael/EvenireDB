using EvenireDB.Common;

namespace EvenireDB.Client
{
    public interface IEventsClient
    {
        //TODO: add response
        ValueTask AppendAsync(Guid streamId, IEnumerable<EventData> events, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Event> ReadAsync(Guid streamId, StreamPosition position, Direction direction = Direction.Forward, CancellationToken cancellationToken = default);
    }
}