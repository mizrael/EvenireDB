public record Settings
{
    public Guid[] SensorIds { get; init; }
    public bool UseGrpc { get; init; } = true; //TODO: this should go on the connection string
}