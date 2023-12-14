using EvenireDB.Server.DTO;
using EvenireDB;

public class EventMapper
{
    public Event[] ToModels(EventDTO[] dtos)
    {
        if (dtos is null)
            throw new ArgumentNullException(nameof(dtos));
        if (dtos.Length == 0)
            return Array.Empty<Event>();

        var events = new Event[dtos.Length];
        for (int i = 0; i < dtos.Length; i++)
        {
            events[i] = ToModel(dtos[i]);
        }
        return events;
    }

    public Event ToModel(EventDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));

        return new Event(dto.Id.ToModel(), dto.Type, dto.Data);
    }
}