using EvenireDB.Common;
using System.Runtime.CompilerServices;

namespace EvenireDB
{
    public interface IEventsProvider
    {
        IAsyncEnumerable<Event> ReadAsync(
            Guid streamId, 
            StreamPosition startPosition,
            Direction direction = Direction.Forward, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default);
    }
}