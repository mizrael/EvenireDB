namespace EvenireDB;

public interface IStreamInfoProvider
{
    IEnumerable<StreamInfo> GetStreamsInfo();
    StreamInfo GetStreamInfo(Guid streamId);
}
