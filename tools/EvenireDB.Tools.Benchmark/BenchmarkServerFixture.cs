using Grpc.Net.Client;

public class BenchmarkServerFixture : IAsyncDisposable
{
    private readonly List<IDisposable> _toDispose = new();

    public BenchmarkServerFactory CreateServer()
    {
        var application = new BenchmarkServerFactory();

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

    public ValueTask DisposeAsync()
    {
        foreach (var disposable in _toDispose)
            disposable.Dispose();
        _toDispose.Clear();

        return ValueTask.CompletedTask;
    }
}
