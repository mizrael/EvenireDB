namespace EvenireDB
{
    public interface IEventsWriter
    {
        ValueTask<IOperationResult> AppendAsync(
            Guid streamId, 
            IEnumerable<EventData> events, 
            int? expectedVersion = null,
            CancellationToken cancellationToken = default);
    }
}