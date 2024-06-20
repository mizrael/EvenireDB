using EvenireDB.Common;

namespace EvenireDB.Client;

public interface IStreamsClient
{
    ValueTask<IEnumerable<StreamInfo>> GetStreamInfosAsync(StreamType? streamsType = null, CancellationToken cancellationToken = default);
    ValueTask<StreamInfo> GetStreamInfoAsync(StreamId streamId, CancellationToken cancellationToken = default);
    ValueTask DeleteStreamAsync(StreamId streamId, CancellationToken cancellationToken = default);
}