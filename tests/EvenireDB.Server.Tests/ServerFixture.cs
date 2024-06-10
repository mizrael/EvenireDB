using Grpc.Net.Client;

namespace EvenireDB.Server.Tests;

public class ServerFixture : IAsyncLifetime
{
    private readonly List<IDisposable> _toDispose = new();

    public WebApplicationFactory<Program> CreateServer()
    {
        var application = new TestServerWebApplicationFactory();

        _toDispose.Add(application);

        return application;
    }

    public GrpcChannel CreateGrpcChannel()
    {
        var application = CreateServer();

        var handler = application.Server.CreateHandler();
        _toDispose.Add(handler);

        var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions()
        {
            HttpHandler = handler,
        });
        _toDispose.Add(channel);

        return channel;
    }

    public Task InitializeAsync()
    => Task.CompletedTask;

    public Task DisposeAsync()
    {
        foreach (var disposable in _toDispose)
            disposable.Dispose();
        _toDispose.Clear();

        return Task.CompletedTask;
    }
}