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

    public async ValueTask DeleteStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/streams/{streamId}", cancellationToken)
                                        .ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return;

        throw response.StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => new StreamNotFoundException(streamId),
            _ => new ClientException(ErrorCodes.Unknown, await response.Content.ReadAsStringAsync().ConfigureAwait(false))
        };    
    }

    public async ValueTask<IEnumerable<StreamInfo>> GetStreamInfosAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/v1/streams", cancellationToken)
                                        .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var results = (await response.Content.ReadFromJsonAsync<StreamInfo[]>(cancellationToken: cancellationToken))
                        ?? Array.Empty<StreamInfo>();
        return results;
    }

    public async ValueTask<StreamInfo> GetStreamInfoAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/streams/{streamId}", cancellationToken)
                                        .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)            
        throw response.StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => new StreamNotFoundException(streamId),
            _ => new ClientException(ErrorCodes.Unknown, await response.Content.ReadAsStringAsync().ConfigureAwait(false))
        };

        return await response.Content.ReadFromJsonAsync<StreamInfo>(cancellationToken: cancellationToken)
                                     .ConfigureAwait(false);

    }
}
