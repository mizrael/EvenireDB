namespace EvenireDB.Extents;

public interface IStreamInfoProvider
{
    ExtentInfo GetExtentInfo(Guid streamId);
    IEnumerable<StreamInfo> GetStreamsInfo();
    StreamInfo GetStreamInfo(Guid streamId);
}