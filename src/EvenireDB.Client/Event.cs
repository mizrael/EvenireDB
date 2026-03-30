namespace EvenireDB.Client;

/// <summary>
/// Represents an event with a unique identifier, type, and associated data. Normally used when reading events from the server.
/// </summary>
public record Event : EventData
{
    public Event(EventId id, string type, ReadOnlyMemory<byte> data) : base(type, data)
    {
        Id = id;
    }

    public EventId Id { get; }
}
