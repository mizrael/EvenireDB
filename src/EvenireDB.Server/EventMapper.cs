using EvenireDB.Server.DTO;
using EvenireDB;

public class EventMapper
{
    private readonly IEventDataValidator _validator;

    public EventMapper(IEventDataValidator validator)
    {
        _validator = validator;
    }

    public EventData[] ToModels(EventDataDTO[] dtos)
    {
        if (dtos is null)
            throw new ArgumentNullException(nameof(dtos));
        if (dtos.Length == 0)
            return [];

        var events = new EventData[dtos.Length];
        for (int i = 0; i < dtos.Length; i++)
        {
            events[i] = ToModel(dtos[i]);
        }
        return events;
    }

    public EventData ToModel(EventDataDTO dto)
    {
        _validator.Validate(dto.Type, dto.Data);        
        return new EventData(dto.Type, dto.Data);
    }
}