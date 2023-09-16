namespace EvenireDB.Tests
{
    public class DataFixture : IAsyncLifetime
    {
        private const string BaseDataPath = "./data/";
        private readonly List<string> _aggregates = new();

        public string CreateAggregateDataFolder(Guid aggregateId)
        {
            var path = Path.Combine(BaseDataPath, aggregateId.ToString());
            Directory.CreateDirectory(path);
            _aggregates.Add(path);
            return path; 
        }

        public Task DisposeAsync()
        {
            foreach(var path in _aggregates)
                Directory.Delete(path, true);
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