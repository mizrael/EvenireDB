using Grpc.Net.Client;

namespace EvenireDB.Server.Tests;

public class ServerFixture : IAsyncLifetime
{
    private struct ServerInfo
    {
        public IDisposable application;
        public string tempDataFolder;
    }

    private readonly List<ServerInfo> _servers = new();
    private readonly List<IDisposable> _toDispose = new();

    public WebApplicationFactory<Program> CreateServer()
    {
        var dataFolder = Directory.CreateTempSubdirectory("eveniredb-tests");

        var application = new TestServerWebApplicationFactory(dataFolder);

        _servers.Add(new ServerInfo()
        {
            application = application,
            tempDataFolder = dataFolder.FullName
        });

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
        foreach(var disposable in _toDispose)
            disposable.Dispose();
        _toDispose.Clear();

        foreach (var instance in _servers)
        {
            instance.application?.Dispose();

            try
            {
                lock (this)
                {
                    if (Directory.Exists(instance.tempDataFolder))
                        Directory.Delete(instance.tempDataFolder, true);
                }
            }
            catch
            {
                // best effort
            }
        }

        _servers.Clear();

        return Task.CompletedTask;
    }
}