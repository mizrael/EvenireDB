using Grpc.Net.Client;

namespace EvenireDB.Server.Tests
{
    public class ServerFixture : IAsyncLifetime
    {
        private readonly List<IDisposable> _instances = new();

        public WebApplicationFactory<Program> CreateServer()
        {
            var application = new WebApplicationFactory<Program>();
            _instances.Add(application);
            return application;
        }

        public GrpcChannel CreateGrpcChannel()
        {
            var application = CreateServer();
            
            var handler = application.Server.CreateHandler();
            _instances.Add(handler);
            
            var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions()
            {
                HttpHandler = handler,
            });
            _instances.Add(channel);

            return channel;
        }

        public Task InitializeAsync()
        => Task.CompletedTask;

        public Task DisposeAsync()
        {
            foreach (var instance in _instances)
                instance.Dispose();
            _instances.Clear();

            if(Directory.Exists("./data"))
                Directory.Delete("./data", true);

            return Task.CompletedTask;
        }
    }
}