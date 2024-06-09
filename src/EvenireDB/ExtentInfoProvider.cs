namespace EvenireDB;

internal record ExtentInfoProviderConfig(string BasePath);

internal class ExtentInfoProvider : IExtentInfoProvider
{
    private readonly ExtentInfoProviderConfig _config;

    public ExtentInfoProvider(ExtentInfoProviderConfig config)
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
            StreamId = streamId,
            DataPath = Path.Combine(_config.BasePath, $"{key}_{extentNumber}_data.dat"),
            HeadersPath = Path.Combine(_config.BasePath, $"{key}_{extentNumber}_headers.dat"),
        };
    }

    public IEnumerable<ExtentInfo> GetExtentsInfo()
    {
        // taking the first extent for each stream
        // just to be sure that the stream exists
        // so that we can parse the stream id from the file name
        var headersFiles = Directory.GetFiles(_config.BasePath, "*_0_headers.dat");
        foreach (var headerFile in headersFiles)
        {
            var filename = Path.GetFileNameWithoutExtension(headerFile);
            var key = filename.Substring(0, 32);
            
            yield return GetExtentInfo(Guid.Parse(key));
        }
    }
}
