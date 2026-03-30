namespace EvenireDB.Samples.TemperatureSensors;

public record Settings
{
    public required Guid[] SensorIds { get; init; }
}
