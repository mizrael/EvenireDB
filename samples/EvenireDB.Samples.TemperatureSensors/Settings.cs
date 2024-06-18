namespace EvenireDB.Samples.TemperatureSensors;

public record Settings
{
    public Guid[] SensorIds { get; init; }
}
