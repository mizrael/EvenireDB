using System.Runtime.InteropServices;

namespace EvenireDB;

internal record StreamInfoProviderConfig(string BasePath);

internal class StreamInfoProvider : IStreamInfoProvider
{
    private readonly StreamInfoProviderConfig _config;
    private readonly int _headerSize = Marshal.SizeOf<RawHeader>();

    public StreamInfoProvider(StreamInfoProviderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        if (!Directory.Exists(_config.BasePath))
            Directory.CreateDirectory(config.BasePath);
    }

    public ExtentInfo GetExtentInfo(Guid streamId)
    {
        // TODO: tests
        var key = streamId.ToString("N");
        int extentNumber = 0; // TODO: calculate
        return new ExtentInfo
        {
            DataPath = Path.Combine(_config.BasePath, $"{key}_{extentNumber}_data.dat"),
            HeadersPath = Path.Combine(_config.BasePath, $"{key}_{extentNumber}_headers.dat"),
        };
    }

    public StreamInfo GetStreamInfo(Guid streamId)
    {   
        var extent = this.GetExtentInfo(streamId);

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
        var headersFiles = Directory.GetFiles(_config.BasePath, "*_headers.dat");
        foreach(var headerFile in headersFiles)
        {
            var key = Path.GetFileNameWithoutExtension(headerFile).Split('_')[0];
            yield return GetStreamInfo(Guid.Parse(key));
        }
    }
}