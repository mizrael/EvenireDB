using EvenireDB.Client.Exceptions;
using EvenireDB.Common;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly: InternalsVisibleTo("EvenireDB.Client.Tests")]
[assembly: InternalsVisibleTo("EvenireDB.Tools.Benchmark")]

namespace EvenireDB.Client;

/// <summary>
/// mostly for debugging and testing purposes. Not intended for production use.
/// use the gRPC client instead.
/// </summary>
internal class HttpEventsClient : IEventsClient
{
    private readonly HttpClient _httpClient;

    public HttpEventsClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async IAsyncEnumerable<Event> ReadAsync(
        StreamId streamId,
        StreamPosition position,
        Direction direction = Direction.Forward,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var endpoint = $"/api/v1/streams/{streamId.Type}/{streamId.Key}/events?pos={position}&dir={(int)direction}";
        using var response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                        .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var results = (await response.Content.ReadFromJsonAsync<Event[]>(cancellationToken: cancellationToken)) ?? [];
        foreach (var item in results)
            yield return item;
    }

    public async ValueTask AppendAsync(
        StreamId streamId,
        IEnumerable<EventData> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events, nameof(events));

        using var ms = new MemoryStream();
        await JsonSerializer.SerializeAsync(ms, events, cancellationToken: cancellationToken)
                             .ConfigureAwait(false);

        ms.Position = 0;

        var endpoint = $"/api/v1/streams/{streamId.Type}/{streamId.Key}/events";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StreamContent(ms)
            {
                Headers =
                {
                    ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                }
            }
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken)
                                        .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.Conflict => new DuplicatedEventException(streamId, responseBody),
                _ => new ClientException(ErrorCodes.Unknown, responseBody)
            };
        }
    }
}