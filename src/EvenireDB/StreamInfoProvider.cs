using System.Runtime.InteropServices;

namespace EvenireDB;

internal class StreamInfoProvider : IStreamInfoProvider
{
    private readonly int _headerSize = Marshal.SizeOf<RawHeader>();
    private readonly IExtentInfoProvider _extentInfoProvider;

    public StreamInfoProvider(IExtentInfoProvider extentInfoProvider)
    {
        _extentInfoProvider = extentInfoProvider ?? throw new ArgumentNullException(nameof(extentInfoProvider));
    }

    public StreamInfo GetStreamInfo(Guid streamId)
    {
        var extent = _extentInfoProvider.GetExtentInfo(streamId);

        var fileInfo = new FileInfo(extent.HeadersPath);
        var headersCount = fileInfo.Length / _headerSize;
        return new StreamInfo(
            streamId,
            headersCount,
            fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc);
    }

    public IEnumerable<StreamInfo> GetStreamsInfo()
    {
        var allExtents = _extentInfoProvider.GetExtentsInfo();
        foreach (var extent in allExtents)
        {
            yield return GetStreamInfo(extent.StreamId);
        }
    }
}