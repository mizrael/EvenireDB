namespace EvenireDB.Client;

public record EvenireClientConfig
{
    public required Uri ServerUri { get; init; }

    public bool UseGrpc { get; init; } = true;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public HttpTransportSettings HttpSettings { get; init; } = new();
    public GrpcTransportSettings GrpcSettings { get; init; } = new();

    public record HttpTransportSettings(int Port = 80);
    public record GrpcTransportSettings(int Port = 5243);
}