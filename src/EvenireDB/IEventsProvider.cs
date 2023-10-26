using EvenireDB.Common;
using System.Runtime.CompilerServices;

namespace EvenireDB
{
    public interface IEventsProvider
    {
        ValueTask<IOperationResult> AppendAsync(Guid streamId, IEnumerable<IEvent> incomingEvents, CancellationToken cancellationToken = default);
        IAsyncEnumerable<IEvent> ReadAsync(Guid streamId, StreamPosition startPosition, Direction direction = Direction.Forward, [EnumeratorCancellation] CancellationToken cancellationToken = default);
    }
}