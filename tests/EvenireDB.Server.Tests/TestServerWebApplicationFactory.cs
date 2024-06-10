using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace EvenireDB.Server.Tests;

internal class TestServerWebApplicationFactory : WebApplicationFactory<Program>
{
    private DirectoryInfo _dataFolder;

    public TestServerWebApplicationFactory(DirectoryInfo dataFolder)
    {
        _dataFolder = dataFolder;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Evenire:DataFolder", _dataFolder.FullName }
            });
        });

        base.ConfigureWebHost(builder);
    }
}
