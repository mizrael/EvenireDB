using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace EvenireDB;

internal class StreamInfoProvider : IStreamInfoProvider
{
    private readonly int _headerSize = Marshal.SizeOf<RawHeader>();
    private readonly IExtentsProvider _extentInfoProvider;
    private readonly IStreamsCache _cache;
    private readonly ILogger<StreamInfoProvider> _logger;

    public StreamInfoProvider(
        IExtentsProvider extentInfoProvider,
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

    public IEnumerable<StreamInfo> GetStreamsInfo(string? streamsType = null)
    {
        var allExtents = _extentInfoProvider.GetAllExtentsInfo(streamsType);        
        foreach (var extent in allExtents)
        {
            var info = GetStreamInfo(extent.StreamId, extent.StreamType);
            if (info is not null)
                yield return info;
        }
    }

    //TODO: I don't like this here
    public async ValueTask DeleteStreamAsync(Guid streamId, string streamType, CancellationToken cancellationToken = default)
    {
        var extent = _extentInfoProvider.GetExtentInfo(streamId, streamType, createIfMissing: false);
        if(extent is null)
            throw new ArgumentException($"Stream '{streamId}' does not exist.", nameof(streamId));

        _logger.StreamDeletionStarted(streamId);

        try
        {
            await _extentInfoProvider.DeleteExtentsAsync(streamId, streamType, cancellationToken);

            _cache.Remove(streamId);
        }
        catch (Exception ex)
        {
            _logger.StreamDeletionFailed(streamId, ex.Message);            
        }

        _logger.StreamDeleted(streamId);
    }
}