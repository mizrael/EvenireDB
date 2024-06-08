namespace EvenireDB.Tests;

public class DataFixture : IAsyncLifetime
{
    private const string BaseDataPath = "./data/";
    private readonly List<StreamInfoProviderConfig> _configs = new();

    internal StreamInfoProviderConfig CreateExtentsConfig(Guid aggregateId)
    {
        var path = Path.Combine(BaseDataPath, aggregateId.ToString());
        Directory.CreateDirectory(path);
        var config = new StreamInfoProviderConfig(path);
        _configs.Add(config);
        return config;
    }

    public Task DisposeAsync()
    {
        foreach (var config in _configs)
            Directory.Delete(config.BasePath, true);
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        if (!Directory.Exists(BaseDataPath))
            Directory.CreateDirectory(BaseDataPath);
        return Task.CompletedTask;
    }

    public Event[] BuildEvents(int count, byte[]? data = null)
        => Enumerable.Range(0, count).Select(i => new Event(new EventId(DateTime.UtcNow.Ticks, i), "lorem", data ?? GenerateRandomData())).ToArray();

    private static byte[] GenerateRandomData()
    {
        var length = Random.Shared.Next(10, 1000);
        var data = new byte[length];
        Random.Shared.NextBytes(data);
        return data;
    }
}