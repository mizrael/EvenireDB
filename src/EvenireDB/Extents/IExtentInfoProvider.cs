namespace EvenireDB.Extents
{
    public interface IExtentInfoProvider
    {
        ExtentInfo GetLatest(Guid streamId);
    }
}