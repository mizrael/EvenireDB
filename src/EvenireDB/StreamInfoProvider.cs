using System.Runtime.InteropServices;
using EvenireDB.Exceptions;
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

    public StreamInfo? GetStreamInfo(Guid streamId, string streamType)
    {
        var extent = _extentInfoProvider.GetExtentInfo(streamId, streamType);
        if (extent is null)
            return null;

        var fileInfo = new FileInfo(extent.HeadersPath);
        if (!fileInfo.Exists)
            return null;

        var headersCount = fileInfo.Length / _headerSize;
        return new StreamInfo(
            streamId,
            streamType,
            headersCount,
            _cache.ContainsKey(streamId),
            fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc);
    }

    public IEnumerable<StreamInfo> GetStreamsInfo(string? streamType = null)
    {
        var allExtents = _extentInfoProvider.GetAllExtentsInfo(streamType);
        List<StreamInfo> results = new();
        foreach (var extent in allExtents)
        {
            var info = GetStreamInfo(extent.StreamId, extent.StreamType);
            if (info is not null)
                results.Add(info!);
        }

        return results;
    }

    public async ValueTask DeleteStreamAsync(Guid streamId, string streamType, CancellationToken cancellationToken = default)
    {
        var extent = _extentInfoProvider.GetExtentInfo(streamId, streamType);
        if (extent is null)
            throw new ArgumentException($"Stream '{streamId}' does not exist.");

        var deleted = false;
        int attempt = 0;

        while (!deleted && attempt < 10 && !cancellationToken.IsCancellationRequested)
        {
            _logger.StreamDeletionAttempt(streamId, attempt);

            try
            {
                // TODO: move these to the extents provider
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

        if (!deleted)
            throw new StreamException(streamId, $"Failed to delete stream '{streamId}'.");

        _logger.StreamDeleted(streamId);
    }
}