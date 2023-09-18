using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("EvenireDB.Benchmark")]
[assembly: InternalsVisibleTo("EvenireDB.Tests")]

public record FileEventsRepositoryConfig(string BasePath, int MaxPageSize = 100);

//TODO: consider add versioning on events file
//TODO: consider adding write locks
public class FileEventsRepository : IDisposable, IEventsRepository
{
    private bool _disposedValue = false;
    
    private readonly ConcurrentDictionary<Guid, EventWriteStreams> _aggregateWriteStreams = new();
    private readonly ConcurrentDictionary<string, byte[]> _eventTypes = new();
    private readonly FileEventsRepositoryConfig _config;

    public FileEventsRepository(FileEventsRepositoryConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (!Directory.Exists(_config.BasePath))
            Directory.CreateDirectory(config.BasePath);
    }

    private EventWriteStreams GetAggregateWriteStream(Guid aggregateId)
    {
        var stream = _aggregateWriteStreams.GetOrAdd(aggregateId, _ =>
        {
            string mainStreamPath = GetAggregateFilePath(aggregateId);
            string offsetIndexPath = GetAggregateFilePath(aggregateId, "_offsets");

            return new EventWriteStreams()
            {
                Main = new FileStream(mainStreamPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read),
                IndexOffset = new FileStream(offsetIndexPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read),
            };
        });

        return stream;
    }

    private string GetAggregateFilePath(Guid aggregateId, string type = "")
    => Path.Combine(_config.BasePath, $"{aggregateId}{type}.dat");

    private async Task<long> GetEventStreamOffsetAsync(Guid aggregateId, int offset, CancellationToken cancellationToken)
    {
        if (offset < 1) 
            return 0;

        string offsetIndexPath = GetAggregateFilePath(aggregateId, "_offsets");
        using var stream = new FileStream(offsetIndexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Position = RawEventIndexOffset.SIZE * offset;

        var indexOffsetBuffer = ArrayPool<byte>.Shared.Rent(RawEventIndexOffset.SIZE);

        await stream.ReadAsync(indexOffsetBuffer, 0, RawEventIndexOffset.SIZE, cancellationToken)
                    .ConfigureAwait(false);

        var streamOffset = RawEventIndexOffset.ParseOffset(indexOffsetBuffer);

        ArrayPool<byte>.Shared.Return(indexOffsetBuffer);

        return streamOffset;
    }

    public async Task<IEnumerable<Event>> ReadAsync(Guid aggregateId, int offset = 0, CancellationToken cancellationToken = default)
    {
        string mainStreamPath = GetAggregateFilePath(aggregateId);
        if (!File.Exists(mainStreamPath))
            return Enumerable.Empty<Event>();

        var results = new List<Event>();

        var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);

        using var stream = new FileStream(mainStreamPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Position = await GetEventStreamOffsetAsync(aggregateId, offset, cancellationToken); 
        
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

            var @event = new Event(eventTypeName, eventData, header.EventIndex);
            results.Add(@event);
        }

        ArrayPool<byte>.Shared.Return(headerBuffer);

        return results;
    }

    public async Task WriteAsync(Guid aggregateId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
        var streams = GetAggregateWriteStream(aggregateId);

        var eventsCount = events.Count();

        var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);
        var indexOffsetBuffer = ArrayPool<byte>.Shared.Rent(RawEventIndexOffset.SIZE);

        for (int i = 0; i < eventsCount; i++)
        {
            var @event = events.ElementAt(i);
            
            var indexOffset = new RawEventIndexOffset
            {
                EventIndex = @event.Index,
                Offset = streams.Main.Position
            };
            indexOffset.CopyTo(indexOffsetBuffer);
            await streams.IndexOffset.WriteAsync(indexOffsetBuffer, 0, RawEventIndexOffset.SIZE, cancellationToken)
                        .ConfigureAwait(false);

            var eventType = _eventTypes.GetOrAdd(@event.Type, _ => Encoding.UTF8.GetBytes(@event.Type));            

            var header = new RawEventHeader
            {
                EventIndex = @event.Index,
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
        await streams.IndexOffset.FlushAsync().ConfigureAwait(false);
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

                    kv.Value.IndexOffset.Close();
                    kv.Value.IndexOffset.Dispose();
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
