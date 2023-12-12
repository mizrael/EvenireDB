namespace EvenireDB.Tests
{
    public class DataFixture : IAsyncLifetime
    {
        private const string BaseDataPath = "./data/";
        private readonly List<FileEventsRepositoryConfig> _configs = new();
        private readonly IEventFactory _factory = new EventFactory(1000);

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


        public IEvent[] BuildEvents(int count, byte[]? data = null)
            => Enumerable.Range(0, count).Select(i => _factory.Create(new EventId(42, 71), "lorem", data ?? GenerateRandomData())).ToArray();

        private static byte[] GenerateRandomData()
        {
            var length = Random.Shared.Next(10, 1000);
            var data = new byte[length];
            Random.Shared.NextBytes(data);
            return data;
        }
    }
}