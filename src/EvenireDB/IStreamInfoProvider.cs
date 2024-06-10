namespace EvenireDB;

public interface IStreamInfoProvider
{
    IEnumerable<StreamInfo> GetStreamsInfo();
    StreamInfo? GetStreamInfo(Guid streamId);
    ValueTask DeleteStreamAsync(Guid streamId, CancellationToken cancellationToken = default);
}
