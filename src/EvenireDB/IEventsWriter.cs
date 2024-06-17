namespace EvenireDB
{
    public interface IEventsWriter
    {
        ValueTask<IOperationResult> AppendAsync(
            Guid streamId, 
            string streamType,
            IEnumerable<EventData> events, 
            int? expectedVersion = null,
            CancellationToken cancellationToken = default);
    }
}