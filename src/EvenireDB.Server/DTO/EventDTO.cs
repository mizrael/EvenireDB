namespace EvenireDB.Server.DTO
{
    public record EventDTO(Guid Id, string Type, ReadOnlyMemory<byte> Data)
    {
        public static EventDTO FromModel(IEvent @event)
        => new EventDTO(@event.Id, @event.Type, @event.Data);
    }
}