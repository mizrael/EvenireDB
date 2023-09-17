namespace EvenireDB.Server.Tests.Routes
{
    public class MainEndpointsTests
    {
        [Fact]
        public async Task Root_endpoint_should_return_ok()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var response = await client.GetAsync("/");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("EvenireDB Server is running!");
        }

        [Fact]
        public async Task Healthcheck_endpoint_should_return_ok()
        {
            await using var application = new WebApplicationFactory<Program>();
            using var client = application.CreateClient();
            var response = await client.GetAsync("/healthz");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }
}