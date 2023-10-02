using System.Net.Http.Json;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("EvenireDB.Client.Tests")]

namespace EvenireDB.Client
{
    internal class HttpEventsClient : IEventsClient
    {
        private readonly HttpClient _httpClient;

        public HttpEventsClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async IAsyncEnumerable<Event> ReadAsync(
            Guid streamId,
            StreamPosition position,
            Direction direction = Direction.Forward,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var endpoint = $"/api/v1/events/{streamId}?pos={position}&dir={(int)direction}";
            var response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                            .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var results = (await response.Content.ReadFromJsonAsync<Event[]>(cancellationToken: cancellationToken))
                            ?? Array.Empty<Event>();
            foreach(var item in results)
                yield return item;
        }

        public async ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/events/{streamId}", events, cancellationToken)
                                            .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return;
            
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.Conflict => new DuplicatedEventException(streamId, responseBody),
                _ => new ClientException(ErrorCodes.Unknown, responseBody)
            };
        }
    }
}