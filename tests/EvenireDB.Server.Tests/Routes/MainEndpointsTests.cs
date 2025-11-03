namespace EvenireDB.Server.Tests.Routes;

public class MainEndpointsTests
{
    [Fact]
    public async Task Root_endpoint_should_return_ok()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var response = await client.GetAsync("/");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);            
    }

    [Fact]
    public async Task Healthcheck_endpoint_should_return_ok()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var response = await client.GetAsync("/healthz");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}