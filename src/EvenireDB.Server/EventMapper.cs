using EvenireDB.Server.DTO;

public class EventMapper
{
    private readonly int _maxEventDataSize;

    public EventMapper(int maxEventDataSize)
    {
        _maxEventDataSize = maxEventDataSize;
    }

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
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));
        if (dto.Data is null)
            throw new ArgumentNullException(nameof(dto.Data));
        if(dto.Data.Length > _maxEventDataSize)
            throw new ArgumentOutOfRangeException(nameof(dto.Data), $"Event data size exceeds the maximum allowed size of {_maxEventDataSize} bytes.");
        return new Event(dto.Id, dto.Type, dto.Data);
    }
}