using EvenireDB.Common;

namespace EvenireDB.Client
{
    public interface IEventsClient
    {
        //TODO: add response
        ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);

        IAsyncEnumerable<PersistedEvent> ReadAsync(Guid streamId, StreamPosition position, Direction direction = Direction.Forward, CancellationToken cancellationToken = default);
    }
}