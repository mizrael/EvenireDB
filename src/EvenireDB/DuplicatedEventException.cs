namespace EvenireDB
{
    public class DuplicatedEventException : Exception
    {
        public DuplicatedEventException(Guid streamId, Event @event) : base($"event '{@event.Id}' is already present on stream '{streamId}'")
        {
            StreamId = streamId;
            Event = @event;
        }

        public Guid StreamId { get; }
        public Event Event { get; }
    }
}