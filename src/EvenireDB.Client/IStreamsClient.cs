namespace EvenireDB.Client;

public interface IStreamsClient
{
    ValueTask<IEnumerable<StreamInfo>> GetStreamInfosAsync(CancellationToken cancellationToken = default);
    ValueTask<StreamInfo> GetStreamInfoAsync(Guid streamId, CancellationToken cancellationToken = default);
    ValueTask DeleteStreamAsync(Guid streamId, CancellationToken cancellationToken = default);
}