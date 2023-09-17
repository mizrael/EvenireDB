public interface IEventsProcessor
{
    void Enqueue(Guid aggregateId, EventDTO[] events);
    Task PersistPendingAsync(CancellationToken cancellationToken = default);
}