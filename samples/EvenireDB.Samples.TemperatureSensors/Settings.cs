using System.Collections.Concurrent;

public record Settings
{
    public Guid[] SensorIds { get; init; }
}