namespace EvenireDB.Extents
{
    public interface IExtentInfoProvider
    {
        ExtentInfo Get(Guid streamId);
    }
}