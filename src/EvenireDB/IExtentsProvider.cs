using EvenireDB.Common;

namespace EvenireDB;

public interface IExtentsProvider
{
    /// <summary>
    /// returns the extent details for the specified stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="streamType">the stream type.</param>
    /// <param name="createIfMissing">when true, will return the first extent for the stream.</param>
    /// <returns></returns>
    ExtentInfo? GetExtentInfo(StreamId streamId, bool createIfMissing = false);

    IEnumerable<ExtentInfo> GetAllExtentsInfo(StreamType? streamType = null);

    ValueTask DeleteExtentsAsync(StreamId streamId, CancellationToken cancellationToken = default);
}