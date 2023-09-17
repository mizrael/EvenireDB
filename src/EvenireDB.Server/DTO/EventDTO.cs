using Microsoft.Extensions.Logging;

public record EventDTO(string Type, byte[] Data, long Index)
{
    public static EventDTO FromModel(Event @event)
    => new EventDTO(@event.Type, @event.Data, @event.Index);
    
    public Event ToModel()
        => new Event(this.Type, this.Data, this.Index);
}
