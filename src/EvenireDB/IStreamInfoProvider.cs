namespace EvenireDB;

public interface IStreamInfoProvider
{
    ExtentInfo GetExtentInfo(Guid streamId);
    IEnumerable<StreamInfo> GetStreamsInfo();
    StreamInfo GetStreamInfo(Guid streamId);
}