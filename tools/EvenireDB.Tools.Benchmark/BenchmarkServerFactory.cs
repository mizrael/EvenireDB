using EvenireDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class BenchmarkServerFactory : WebApplicationFactory<Program>
{
    private readonly DirectoryInfo _dataFolder;

    public BenchmarkServerFactory()
    {
        _dataFolder = Directory.CreateTempSubdirectory("eveniredb-benchmarks");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Evenire:DataFolder", _dataFolder.FullName },
                { "Evenire:HttpSettings:Port", Random.Shared.Next(8000, int.MaxValue).ToString() },
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        });

        base.ConfigureWebHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        lock (this)
        {
            try
            {
                if (_dataFolder.Exists)
                    _dataFolder.Delete(true);
            }
            catch
            {
                // best effort
            }
        }
    }
}
