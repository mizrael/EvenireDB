namespace EvenireDB.Server.DTO
{
    public record EventDTO(EventIdDTO Id, string Type, ReadOnlyMemory<byte> Data)
    {
        public static EventDTO FromModel(Event @event)
        => new EventDTO(EventIdDTO.FromModel(@event.Id), @event.Type, @event.Data);
    }
}