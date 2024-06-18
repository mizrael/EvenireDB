namespace EvenireDB;

public interface IStreamInfoProvider
{
    IEnumerable<StreamInfo> GetStreamsInfo(string? streamsType = null);
    StreamInfo? GetStreamInfo(Guid streamId, string streamType);
    ValueTask DeleteStreamAsync(Guid streamId, string streamType, CancellationToken cancellationToken = default);
}
