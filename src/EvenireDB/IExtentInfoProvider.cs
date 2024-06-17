namespace EvenireDB;

public interface IExtentInfoProvider
{
    /// <summary>
    /// returns the extent details for the specified stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="streamType">the stream type.</param>
    /// <param name="createIfMissing">when true, will return the first extent for the stream.</param>
    /// <returns></returns>
    ExtentInfo? GetExtentInfo(Guid streamId, string streamType, bool createIfMissing = false);

    IEnumerable<ExtentInfo> GetAllExtentsInfo(string? streamType = null);
}