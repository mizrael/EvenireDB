namespace EvenireDB;

public interface IExtentInfoProvider
{
    ExtentInfo? GetExtentInfo(Guid streamId);
    IEnumerable<ExtentInfo> GetExtentsInfo();
}