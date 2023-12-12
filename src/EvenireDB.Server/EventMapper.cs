using EvenireDB.Server.DTO;
using EvenireDB;

public class EventMapper
{
    private readonly IEventFactory _factory;

    public EventMapper(IEventFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public IEvent[] ToModels(EventDTO[] dtos)
    {
        if (dtos is null)
            throw new ArgumentNullException(nameof(dtos));
        if (dtos.Length == 0)
            return Array.Empty<IEvent>();

        var events = new IEvent[dtos.Length];
        for (int i = 0; i < dtos.Length; i++)
        {
            events[i] = ToModel(dtos[i]);
        }
        return events;
    }

    public IEvent ToModel(EventDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));

        return _factory.Create(dto.Id.ToModel(), dto.Type, dto.Data);
    }
}