namespace EvenireDB.Client
{
    public interface IEventsClient
    {
        Task AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default);
        Task<IEnumerable<Event>> GetEventsAsync(Guid streamId, int skip = 0, CancellationToken cancellationToken = default);
    }
}