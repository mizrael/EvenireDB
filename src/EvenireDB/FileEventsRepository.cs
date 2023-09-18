using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("EvenireDB.Benchmark")]
[assembly: InternalsVisibleTo("EvenireDB.Tests")]

public record FileEventsRepositoryConfig(string BasePath, int MaxPageSize = 100);

//TODO: consider add versioning on events file
//TODO: consider adding write locks to handle concurrency
//TODO: make sure event IDs are unique in a stream

public class FileEventsRepository : IDisposable, IEventsRepository
{
    private bool _disposedValue = false;
    
    // TODO: this should be a LRU-cache
    private readonly ConcurrentDictionary<Guid, EventWriteStreams> _aggregateWriteStreams = new();
    private readonly ConcurrentDictionary<string, byte[]> _eventTypes = new();
    private readonly FileEventsRepositoryConfig _config;

    public FileEventsRepository(FileEventsRepositoryConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (!Directory.Exists(_config.BasePath))
            Directory.CreateDirectory(config.BasePath);
    }

    private EventWriteStreams GetEventsStream(Guid streamId)
    {
        var stream = _aggregateWriteStreams.GetOrAdd(streamId, _ =>
        {
            string mainStreamPath = GetStreamPath(streamId);
            string offsetIndexPath = GetStreamPath(streamId, "_offsets");

            return new EventWriteStreams()
            {
                Main = new FileStream(mainStreamPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read),
                OffsetIndex = new FileStream(offsetIndexPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read),
            };
        });

        return stream;
    }

    private string GetStreamPath(Guid streamId, string type = "")
    => Path.Combine(_config.BasePath, $"{streamId}{type}.dat");

    private async Task<RawEventOffsetIndex> GetEventIndex(Guid streamId, int skip = 0, CancellationToken cancellationToken = default)
    {
        if (skip < 0) 
            throw new ArgumentOutOfRangeException(nameof(skip));

        string offsetIndexPath = GetStreamPath(streamId, "_offsets");
        using var stream = new FileStream(offsetIndexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Position = RawEventOffsetIndex.SIZE * skip;

        var indexOffsetBuffer = ArrayPool<byte>.Shared.Rent(RawEventOffsetIndex.SIZE);

        await stream.ReadAsync(indexOffsetBuffer, 0, RawEventOffsetIndex.SIZE, cancellationToken)
                    .ConfigureAwait(false);

        var index = new RawEventOffsetIndex();
        RawEventOffsetIndex.Parse(indexOffsetBuffer, ref index);

        ArrayPool<byte>.Shared.Return(indexOffsetBuffer);

        return index;
    }

    public async Task<IEnumerable<Event>> ReadAsync(Guid streamId, int skip = 0, CancellationToken cancellationToken = default)
    {
        string mainStreamPath = GetStreamPath(streamId);
        if (!File.Exists(mainStreamPath))
            return Enumerable.Empty<Event>();

        var results = new List<Event>();

        var index = await GetEventIndex(streamId, skip, cancellationToken).ConfigureAwait(false); 
        
        var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);        

        using var stream = new FileStream(mainStreamPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Position = index.MainStreamPosition;

        while (true)
        {
            if (results.Count >= _config.MaxPageSize)
                break;

            var bytesRead = await stream.ReadAsync(headerBuffer, 0, RawEventHeader.SIZE, cancellationToken)
                                        .ConfigureAwait(false);
            if (bytesRead == 0)
                break;

            var header = new RawEventHeader();
            RawEventHeader.Parse(headerBuffer, ref header);

            var eventTypeNameBuff = ArrayPool<byte>.Shared.Rent(header.EventTypeNameLength);

            await stream.ReadAsync(eventTypeNameBuff, 0, header.EventTypeNameLength, cancellationToken)
                        .ConfigureAwait(false);

            // have to create a span to ensure that we're parsing the right buffer size
            // as ArrayPool might return a buffer with size the closest pow(2)
            var eventTypeName = Encoding.UTF8.GetString(eventTypeNameBuff.AsSpan(0, header.EventTypeNameLength));

            ArrayPool<byte>.Shared.Return(eventTypeNameBuff);

            var eventData = new byte[header.EventDataLength];

            await stream.ReadAsync(eventData, 0, header.EventDataLength, cancellationToken)
                                  .ConfigureAwait(false);

            var @event = new Event(header.EventId, eventTypeName, eventData);
            results.Add(@event);
        }

        ArrayPool<byte>.Shared.Return(headerBuffer);

        return results;
    }

    public async Task WriteAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
        var streams = GetEventsStream(streamId);

        var eventsCount = events.Count();

        var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);
        var indexOffsetBuffer = ArrayPool<byte>.Shared.Rent(RawEventOffsetIndex.SIZE);

        for (int i = 0; i < eventsCount; i++)
        {
            var @event = events.ElementAt(i);
            
            var indexOffset = new RawEventOffsetIndex
            {
                EventId = @event.Id,
                MainStreamPosition = streams.Main.Position,
                EventDataSize = @event.Data.Length,
            };
            indexOffset.CopyTo(indexOffsetBuffer);
            await streams.OffsetIndex.WriteAsync(indexOffsetBuffer, 0, RawEventOffsetIndex.SIZE, cancellationToken)
                        .ConfigureAwait(false);

            var eventType = _eventTypes.GetOrAdd(@event.Type, _ => Encoding.UTF8.GetBytes(@event.Type));            

            var header = new RawEventHeader
            {
                EventId = @event.Id,
                EventTypeNameLength = eventType.Length,
                EventDataLength = @event.Data.Length,
            };
            header.CopyTo(headerBuffer);
            await streams.Main.WriteAsync(headerBuffer, 0, RawEventHeader.SIZE, cancellationToken)
                              .ConfigureAwait(false);

            await streams.Main.WriteAsync(eventType, cancellationToken).ConfigureAwait(false);
            await streams.Main.WriteAsync(@event.Data, cancellationToken).ConfigureAwait(false);            
        }

        ArrayPool<byte>.Shared.Return(headerBuffer);
        ArrayPool<byte>.Shared.Return(indexOffsetBuffer);

        await streams.Main.FlushAsync().ConfigureAwait(false);
        await streams.OffsetIndex.FlushAsync().ConfigureAwait(false);
    }

    #region Disposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                foreach (var kv in _aggregateWriteStreams)
                {
                    kv.Value.Main.Close();
                    kv.Value.Main.Dispose();

                    kv.Value.OffsetIndex.Close();
                    kv.Value.OffsetIndex.Dispose();
                }

                _aggregateWriteStreams.Clear();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion Disposable
}
