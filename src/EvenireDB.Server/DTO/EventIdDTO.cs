namespace EvenireDB.Server.DTO
{
    public record EventIdDTO(long Timestamp, int Sequence)
    {
        public static EventIdDTO FromModel(EventId eventId)
        => new EventIdDTO(eventId.Timestamp, eventId.Sequence);

        public EventId ToModel()
        => new EventId(this.Timestamp, this.Sequence);
    }
}