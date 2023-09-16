namespace EvenireDB.Tests
{
    public class DataFixture : IAsyncLifetime
    {
        private const string BaseDataPath = "./data/";
        private readonly List<FileEventsRepositoryConfig> _configs = new();

        public FileEventsRepositoryConfig CreateRepoConfig(Guid aggregateId)
        {
            var path = Path.Combine(BaseDataPath, aggregateId.ToString());
            Directory.CreateDirectory(path);
            var config = new FileEventsRepositoryConfig(path);
            _configs.Add(config);
            return config; 
        }

        public Task DisposeAsync()
        {
            foreach(var config in _configs)
                Directory.Delete(config.BasePath, true);
            return Task.CompletedTask;
        }

        public Task InitializeAsync()
        {
            if (!Directory.Exists(BaseDataPath))
                Directory.CreateDirectory(BaseDataPath);
            return Task.CompletedTask;
        }
    }
}