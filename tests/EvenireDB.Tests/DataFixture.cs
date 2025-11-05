namespace EvenireDB.Tests;

public class DataFixture : IAsyncLifetime
{
    private DirectoryInfo? _baseDataPath;

    internal ExtentsProviderConfig CreateExtentsConfig()
    {        
        if(_baseDataPath is null)
            throw new InvalidOperationException("Fixture not initialized");

        var path = Path.Combine(_baseDataPath.FullName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);

        var config = new ExtentsProviderConfig(path);        
        return config;
    }

    public Task DisposeAsync()
    {
        lock (this)
        {
            try
            {
                if (_baseDataPath?.Exists == true)
                    _baseDataPath.Delete(true);
            }
            catch
            {
                // best effort
            }
        }

        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        _baseDataPath = Directory.CreateTempSubdirectory("eveniredb-tests");        
        return Task.CompletedTask;
    }

    public Event[] BuildEvents(int count, byte[]? data = null)
        => Enumerable.Range(0, count).Select(i => new Event(new EventId(DateTime.UtcNow.Ticks, i), "lorem", data ?? GenerateRandomData())).ToArray();

    public static byte[] GenerateRandomData(int minSize = 10, int maxSize = 1000)
    {
        var length = Random.Shared.Next(minSize, maxSize);
        var data = new byte[length];
        Random.Shared.NextBytes(data);
        return data;
    }
}