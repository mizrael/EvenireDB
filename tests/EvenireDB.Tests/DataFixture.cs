namespace EvenireDB.Tests;

public class DataFixture : IAsyncLifetime
{
    private DirectoryInfo _baseDataPath;

    internal ExtentsProviderConfig CreateExtentsConfig()
    {        
        var path = Path.Combine(_baseDataPath.FullName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);

        var config = new ExtentsProviderConfig(path);        
        return config;
    }

    public Task DisposeAsync()
    {
        try
        {
            lock (this)
            {
                if (_baseDataPath.Exists)
                    _baseDataPath.Delete(true);
            }
        }
        catch
        {
            // best effort
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

    private static byte[] GenerateRandomData()
    {
        var length = Random.Shared.Next(10, 1000);
        var data = new byte[length];
        Random.Shared.NextBytes(data);
        return data;
    }
}