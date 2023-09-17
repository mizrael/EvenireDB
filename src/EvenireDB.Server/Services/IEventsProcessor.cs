using EvenireDB.Server.DTO;

namespace EvenireDB.Server.Services
{
    public interface IEventsProcessor
    {
        void Enqueue(Guid aggregateId, EventDTO[] events);
        Task PersistPendingAsync(CancellationToken cancellationToken = default);
    }
}