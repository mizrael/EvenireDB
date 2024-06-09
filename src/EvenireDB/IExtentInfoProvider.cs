namespace EvenireDB;

public interface IExtentInfoProvider
{
    ExtentInfo? GetExtentInfo(Guid streamId, bool skipCheck = false);
    IEnumerable<ExtentInfo> GetExtentsInfo();
}