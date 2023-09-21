using System.Net.Http.Json;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("EvenireDB.Client.Tests")]

namespace EvenireDB.Client
{
    internal class EventsClient : IEventsClient
    {
        private readonly HttpClient _httpClient;

        public EventsClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<IEnumerable<Event>> GetEventsAsync(Guid streamId, int skip = 0, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"/api/v1/events/{streamId}?skip={skip}", HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                            .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadFromJsonAsync<Event[]>(cancellationToken: cancellationToken);
            return results ?? Enumerable.Empty<Event>();
        }

        public async Task AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/events/{streamId}", events, cancellationToken)
                                            .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }
}