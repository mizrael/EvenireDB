using EvenireDB.Common;

namespace EvenireDB;

public interface IEventsWriter
{
    ValueTask<IOperationResult> AppendAsync(
        StreamId streamId, 
        IEnumerable<EventData> events, 
        int? expectedVersion = null,
        CancellationToken cancellationToken = default);
}