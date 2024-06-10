using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace EvenireDB.Server.Tests;

public class TestServerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DirectoryInfo _dataFolder;

    public TestServerWebApplicationFactory()
    {
        _dataFolder = Directory.CreateTempSubdirectory("eveniredb-tests");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Evenire:DataFolder", _dataFolder.FullName },
                { "Evenire:HttpSettings:Port", Random.Shared.Next(8000, int.MaxValue).ToString() },
            });
        });

        base.ConfigureWebHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            lock (this)
            {
                if (_dataFolder.Exists)
                    _dataFolder.Delete(true);
            }
        }
        catch
        {
            // best effort
        }
    }
}
