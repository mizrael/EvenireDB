using EvenireDB.Common;

namespace EvenireDB;

public interface IStreamInfoProvider
{
    IEnumerable<StreamInfo> GetStreamsInfo(StreamType? streamsType = null);
    StreamInfo? GetStreamInfo(StreamId streamId);
    
    ValueTask<bool> DeleteStreamAsync(StreamId streamId, CancellationToken cancellationToken = default);
}
