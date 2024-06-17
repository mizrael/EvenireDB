namespace EvenireDB;

internal record ExtentInfoProviderConfig(string BasePath);

// TODO: tests
internal class ExtentInfoProvider : IExtentInfoProvider
{
    private readonly ExtentInfoProviderConfig _config;

    public ExtentInfoProvider(ExtentInfoProviderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        if (!Directory.Exists(_config.BasePath))
            Directory.CreateDirectory(config.BasePath);
    }
        
    public ExtentInfo? GetExtentInfo(Guid streamId, string streamType, bool createIfMissing = false)
    {
        if (string.IsNullOrWhiteSpace(streamType))        
            throw new ArgumentException($"'{nameof(streamType)}' cannot be null or whitespace.", nameof(streamType));        
        
        var key = streamId.ToString("N");
        int extentNumber = 0; // TODO: calculate

        var typedExtentsPath = Path.Combine(_config.BasePath, streamType);

        lock (this)
        {
            if (!Directory.Exists(typedExtentsPath))
                Directory.CreateDirectory(typedExtentsPath);
        }

        var headersPath = Path.Combine(typedExtentsPath, $"{key}_{extentNumber}_headers.dat");
        if(!createIfMissing && !File.Exists(headersPath))
            return null;
        
        return new ExtentInfo
        (
            StreamId: streamId,
            StreamType: streamType,
            DataPath: Path.Combine(_config.BasePath, $"{key}_{extentNumber}_data.dat"),
            HeadersPath: headersPath
        );
    }

    public IEnumerable<ExtentInfo> GetAllExtentsInfo(string? streamType = null)
    {
        var types = Directory.GetDirectories(_config.BasePath, streamType ?? string.Empty) ?? Array.Empty<string>();
        foreach (var typeFound in types)
        {
            // taking the first extent for each stream
            // just to be sure that the stream exists
            // so that we can parse the stream id from the file name
            var headersFiles = Directory.GetFiles(_config.BasePath, "*_0_headers.dat");
            foreach (var headerFile in headersFiles)
            {
                var filename = Path.GetFileNameWithoutExtension(headerFile);
                var key = filename.Substring(0, 32);

                yield return GetExtentInfo(Guid.Parse(key), typeFound!);
            }
        }
    }
}
