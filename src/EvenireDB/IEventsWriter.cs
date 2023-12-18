namespace EvenireDB
{
    public interface IEventsWriter
    {
        ValueTask<IOperationResult> AppendAsync(Guid streamId, IEnumerable<EventData> events, CancellationToken cancellationToken = default);
    }
}