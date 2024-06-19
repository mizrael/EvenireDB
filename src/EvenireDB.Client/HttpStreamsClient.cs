using EvenireDB.Client.Exceptions;
using EvenireDB.Common;
using System.Net.Http.Json;

namespace EvenireDB.Client;

internal class HttpStreamsClient : IStreamsClient
{
    private readonly HttpClient _httpClient;

    public HttpStreamsClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async ValueTask DeleteStreamAsync(StreamId streamId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/streams/{streamId.Type}/{streamId.Key}", cancellationToken)
                                        .ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return;

        throw response.StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => new StreamNotFoundException(streamId),
            _ => new ClientException(ErrorCodes.Unknown, await response.Content.ReadAsStringAsync().ConfigureAwait(false))
        };    
    }

    public async ValueTask<IEnumerable<StreamInfo>> GetStreamInfosAsync(StreamType? streamsType, CancellationToken cancellationToken = default)
    {
        var typeParam = streamsType is not null ? $"?streamsType={streamsType}" : string.Empty;
        var response = await _httpClient.GetAsync($"/api/v1/streams{typeParam}", cancellationToken)
                                        .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var results = (await response.Content.ReadFromJsonAsync<StreamInfo[]>(cancellationToken: cancellationToken)) ?? [];
        return results;
    }

    public async ValueTask<StreamInfo> GetStreamInfoAsync(StreamId streamId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/streams/{streamId.Type}/{streamId.Key}", cancellationToken)
                                        .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)            
            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => new StreamNotFoundException(streamId),
                _ => new ClientException(ErrorCodes.Unknown, await response.Content.ReadAsStringAsync().ConfigureAwait(false))
            };

        var result = await response.Content.ReadFromJsonAsync<StreamInfo>(cancellationToken: cancellationToken)
                                           .ConfigureAwait(false);
        return result!;
    }
}
