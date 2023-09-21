namespace EvenireDB.Server.Tests
{
    public class ServerFixture : IAsyncLifetime
    {
        private readonly List<WebApplicationFactory<Program>> _instances = new();

        public WebApplicationFactory<Program> CreateServer()
        {
            var application = new WebApplicationFactory<Program>();
            _instances.Add(application);
            return application;
        }

        public Task InitializeAsync()
        => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            Directory.Delete("./data", true);

            foreach (var instance in _instances)
                await instance.DisposeAsync();
            _instances.Clear();
        }
    }
}