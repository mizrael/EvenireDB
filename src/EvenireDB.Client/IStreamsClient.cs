namespace EvenireDB.Client;

public interface IStreamsClient
{
    ValueTask<IEnumerable<StreamInfo>> GetStreamInfosAsync(string? streamsType = null, CancellationToken cancellationToken = default);
    ValueTask<StreamInfo> GetStreamInfoAsync(Guid streamId, string streamType, CancellationToken cancellationToken = default);
    ValueTask DeleteStreamAsync(Guid streamId, string streamType, CancellationToken cancellationToken = default);
}