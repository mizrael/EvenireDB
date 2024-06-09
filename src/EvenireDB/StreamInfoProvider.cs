using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace EvenireDB;

internal class StreamInfoProvider : IStreamInfoProvider
{
    private readonly int _headerSize = Marshal.SizeOf<RawHeader>();
    private readonly IExtentInfoProvider _extentInfoProvider;
    private readonly IStreamsCache _cache;
    private readonly ILogger<StreamInfoProvider> _logger;

    public StreamInfoProvider(
        IExtentInfoProvider extentInfoProvider,
        IStreamsCache cache,
        ILogger<StreamInfoProvider> logger)
    {
        _extentInfoProvider = extentInfoProvider ?? throw new ArgumentNullException(nameof(extentInfoProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public StreamInfo? GetStreamInfo(Guid streamId)
    {
        var extent = _extentInfoProvider.GetExtentInfo(streamId);
        if (extent is null)
            return null;

        var fileInfo = new FileInfo(extent.HeadersPath);
        if (!fileInfo.Exists)
            return null;

        var headersCount = fileInfo.Length / _headerSize;
        return new StreamInfo(
            streamId,
            headersCount,
            _cache.ContainsKey(streamId),
            fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc);
    }

    public IEnumerable<StreamInfo> GetStreamsInfo()
    {
        var allExtents = _extentInfoProvider.GetExtentsInfo();
        List<StreamInfo> results = new();
        foreach (var extent in allExtents)
        {
            var info = GetStreamInfo(extent.StreamId);
            if (info is not null)
                results.Add(info!);
        }

        return results;
    }

    public async ValueTask DeleteStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        var extent = _extentInfoProvider.GetExtentInfo(streamId);
        var deleted = false;
        int attempt = 0;

        while (!deleted && attempt < 10 && !cancellationToken.IsCancellationRequested)
        {
            _logger.StreamDeletionAttempt(streamId, attempt);

            try
            {
                if (File.Exists(extent.HeadersPath))
                    File.Delete(extent.HeadersPath);

                if (File.Exists(extent.DataPath))
                    File.Delete(extent.DataPath);

                _cache.Remove(streamId);

                deleted = true;
            }
            catch (Exception ex)
            {
                _logger.StreamDeletionFailed(streamId, ex.Message);
                attempt++;
                await Task.Delay(1000);
            }
        }

        if (deleted)
            _logger.StreamDeleted(streamId);
    }
}