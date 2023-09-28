namespace EvenireDB.Client
{
    public interface IEventsClient
    {
        Task AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
        Task<IEnumerable<Event>> GetAsync(Guid streamId, int startPosition = 0, CancellationToken cancellationToken = default);
    }
}