namespace EvenireDB.Client
{
    public interface IEventsClient
    {
        ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Event> ReadAsync(Guid streamId, StreamPosition position, Direction direction = Direction.Forward, CancellationToken cancellationToken = default);
    }

    internal class GrpcEventsClient : IEventsClient
    {
        public ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<Event> ReadAsync(Guid streamId, StreamPosition position, Direction direction = Direction.Forward, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}