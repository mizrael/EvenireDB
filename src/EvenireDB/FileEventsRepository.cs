using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection.PortableExecutable;
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
            string dataPath = GetStreamPath(streamId, "_data");
            string headersPath = GetStreamPath(streamId, "_headers");

            return new EventWriteStreams()
            {
                Data = new FileStream(dataPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read),
                Headers = new FileStream(headersPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read),
            };
        });

        return stream;
    }

    private string GetStreamPath(Guid streamId, string type)
    => Path.Combine(_config.BasePath, $"{streamId}{type}.dat");

    public async Task<IEnumerable<Event>> ReadAsync(Guid streamId, int skip = 0, CancellationToken cancellationToken = default)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip));

        string headersPath = GetStreamPath(streamId, "_headers");
        string dataPath = GetStreamPath(streamId, "_data");
        if (!File.Exists(headersPath) || !File.Exists(dataPath))
            return Enumerable.Empty<Event>();

        // first, read all the headers, which are stored sequentially
        // this will allow later on pulling the data for all the events in one go
        
        var headers = new List<RawEventHeader>();

        var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);

        using var headersStream = new FileStream(headersPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        headersStream.Position = RawEventHeader.SIZE * skip;

        int dataBufferSize = 0; // TODO: this should probably be a long, but it would then require chunking from the data stream

        while (true)
        {
            if (headers.Count >= _config.MaxPageSize)
                break;

            var bytesRead = await headersStream.ReadAsync(headerBuffer, 0, RawEventHeader.SIZE, cancellationToken)
                                               .ConfigureAwait(false);
            if (bytesRead == 0)
                break;

            var header = new RawEventHeader();
            RawEventHeader.Parse(headerBuffer, ref header);
            headers.Add(header);

            dataBufferSize += header.EventDataLength;
        }

        ArrayPool<byte>.Shared.Return(headerBuffer);

        // now we read the data for all the events in a single buffer
        // so that we can parse it directly, avoiding accessing the file any longer

        var results = new List<Event>();
        using var dataStream = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        dataStream.Position = headers[0].DataPosition;

        var dataBuffer = ArrayPool<byte>.Shared.Rent(dataBufferSize);
        await dataStream.ReadAsync(dataBuffer, 0, dataBufferSize, cancellationToken)
                        .ConfigureAwait(false);

        for (int i = 0; i < headers.Count; i++)
        {
            // have to create a span to ensure that we're parsing the right buffer size
            // as ArrayPool might return a buffer with size the closest pow(2)
            var eventTypeName = Encoding.UTF8.GetString(headers[i].EventType, 0, headers[i].EventTypeLength);

            // when paging, we need to take into account the position of the first block of data
            var eventData = new byte[headers[i].EventDataLength];
            long srcOffset = headers[i].DataPosition - headers[0].DataPosition; 
            Array.Copy(dataBuffer, srcOffset, eventData, 0, headers[i].EventDataLength);

            var @event = new Event(headers[i].EventId, eventTypeName, eventData);
            results.Add(@event);
        }

        ArrayPool<byte>.Shared.Return(dataBuffer);

        return results;
    }

    public async Task WriteAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
        var streams = GetEventsStream(streamId);

        var eventsCount = events.Count();

        var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);
        
        for (int i = 0; i < eventsCount; i++)
        {
            var @event = events.ElementAt(i);
            
            var eventType = _eventTypes.GetOrAdd(@event.Type, _ => {
                var src = Encoding.UTF8.GetBytes(@event.Type); 
                var dest = new byte[Constants.MAX_EVENT_TYPE_LENGTH];                
                Array.Copy(src, dest, src.Length);
                return dest;
            });            

            var header = new RawEventHeader
            {
                EventId = @event.Id,
                EventType = eventType,
                DataPosition = streams.Data.Position,
                EventDataLength = @event.Data.Length,
                EventTypeLength = (short)@event.Type.Length,
            };
            header.CopyTo(headerBuffer);
            await streams.Headers.WriteAsync(headerBuffer, 0, RawEventHeader.SIZE, cancellationToken)
                                 .ConfigureAwait(false);

            await streams.Data.WriteAsync(@event.Data, cancellationToken).ConfigureAwait(false);            
        }

        ArrayPool<byte>.Shared.Return(headerBuffer);        

        await streams.Data.FlushAsync().ConfigureAwait(false);
        await streams.Headers.FlushAsync().ConfigureAwait(false);
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
                    kv.Value.Data.Close();
                    kv.Value.Data.Dispose();

                    kv.Value.Headers.Close();
                    kv.Value.Headers.Dispose();
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
