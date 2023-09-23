using EvenireDB.Server.DTO;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace EvenireDB.Server.Tests.Routes
{
    public class EventsV1EndpointTests : IClassFixture<ServerFixture>
    {
        private readonly static byte[] _defaultEventData = new byte[] { 0x42 };
        private readonly ServerFixture _serverFixture;

        public EventsV1EndpointTests(ServerFixture serverFixture)
        {
            _serverFixture = serverFixture;
        }

        [Fact]
        public async Task Get_Archive_should_be_empty_when_no_events_available_for_aggregate()
        {
            await using var application = _serverFixture.CreateServer();

            using var client = application.CreateClient();
            var response = await client.GetAsync($"/api/v1/events/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var events = await response.Content.ReadFromJsonAsync<EventDTO[]>();
            events.Should().NotBeNull().And.BeEmpty();  
        }

        [Fact]
        public async Task Get_Archive_should_return_events_for_aggregate()
        {
            var streamId = Guid.NewGuid();

            await using var application = _serverFixture.CreateServer();

            var expectedEvents = this.BuildEvents(10);
            var provider = application.Services.GetService<EventsProvider>();
            await provider.AppendAsync(streamId, expectedEvents);

            using var client = application.CreateClient();
            var response = await client.GetAsync($"/api/v1/events/{streamId}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var events = await response.Content.ReadFromJsonAsync<DTO.EventDTO[]>();
            events.Should().BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task Post_should_return_bad_request_when_input_null()
        {
            var streamId = Guid.NewGuid();

            await using var application = _serverFixture.CreateServer();
            using var client = application.CreateClient();
            var nullPayloadResponse = await client.PostAsJsonAsync<EventDTO[]>($"/api/v1/events/{streamId}", null);
            nullPayloadResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Post_should_return_bad_request_when_input_invalid()
        {
            var streamId = Guid.NewGuid();

            await using var application = _serverFixture.CreateServer();
            using var client = application.CreateClient();

            var dtos = BuildEventsDTOs(10, null);
            var nullDataResponse = await client.PostAsJsonAsync<EventDTO[]>($"/api/v1/events/{streamId}", dtos);
            nullDataResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Post_should_return_conflict_when_input_already_in_stream()
        {
            var streamId = Guid.NewGuid();

            await using var application = _serverFixture.CreateServer();
            using var client = application.CreateClient();

            var dtos = BuildEventsDTOs(10, _defaultEventData);
            var firstResponse = await client.PostAsJsonAsync<EventDTO[]>($"/api/v1/events/{streamId}", dtos);
            firstResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

            var errorResponse = await client.PostAsJsonAsync<EventDTO[]>($"/api/v1/events/{streamId}", dtos);
            errorResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Post_should_return_accepted_when_input_valid()
        {
            var streamId = Guid.NewGuid();

            var dtos = BuildEventsDTOs(10, _defaultEventData);

            await using var application = _serverFixture.CreateServer();
            using var client = application.CreateClient();
            var response = await client.PostAsJsonAsync<EventDTO[]>($"/api/v1/events/{streamId}", dtos);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        }

        [Fact]
        public async Task Post_should_create_events()
        {
            var streamId = Guid.NewGuid();

            var dtos = BuildEventsDTOs(10, _defaultEventData);

            await using var application = _serverFixture.CreateServer();
            using var client = application.CreateClient();
            await client.PostAsJsonAsync<EventDTO[]>($"/api/v1/events/{streamId}", dtos);

            var response = await client.GetAsync($"/api/v1/events/{streamId}");            
            var fetchedEvents = await response.Content.ReadFromJsonAsync<EventDTO[]>();
            fetchedEvents.Should().BeEquivalentTo(dtos);
        }

        private Event[] BuildEvents(int count)
            => Enumerable.Range(0, count).Select(i => new Event(Guid.NewGuid(), "lorem", _defaultEventData)).ToArray();

        private EventDTO[] BuildEventsDTOs(int count, byte[]? data)
           => Enumerable.Range(0, count).Select(i => new EventDTO(Guid.NewGuid(), "lorem", data)).ToArray();
    }
}