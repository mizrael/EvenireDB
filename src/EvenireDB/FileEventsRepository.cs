using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

[assembly: InternalsVisibleTo("EvenireDB.Console")]
[assembly: InternalsVisibleTo("EvenireDB.Tests")]

internal class FileEventsRepository : IDisposable
{
    private bool _disposedValue = false;
    private readonly string _basePath;
    private readonly Dictionary<Guid, Stream> _aggregateWriteStreams = new();
    private readonly Dictionary<string, SerializedString> _eventTypes = new ();

    private const int _baseHeaderLength =
        sizeof(int) + // version
        sizeof(long) + // event index
        sizeof(int) + // event type length
        sizeof(int) // event data length
    ;

    private const int ReadBufferLength = 0x1000;

    public FileEventsRepository(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException($"'{nameof(basePath)}' cannot be null or whitespace.", nameof(basePath));
        }

        _basePath = basePath;
    }

    private Stream GetAggregateWriteStream(Guid aggregateId)
    {
        if (!_aggregateWriteStreams.TryGetValue(aggregateId, out Stream stream))
        {
            string aggregatePath = GetAggregateFilePath(aggregateId);
            stream = new FileStream(aggregatePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            _aggregateWriteStreams.Add(aggregateId, stream);
        }

        return stream;
    }

    private string GetAggregateFilePath(Guid aggregateId)
    => Path.Combine(_basePath, aggregateId.ToString() + ".dat");

    public async Task<IEnumerable<Event>> ReadAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        string aggregatePath = GetAggregateFilePath(aggregateId);
        if(!File.Exists(aggregatePath)) 
            return Enumerable.Empty<Event>();

        var results = new List<Event>();

        var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.HEADER_SIZE);
        
        using var stream = new FileStream(aggregatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        while (true)
        {
            var bytesRead = await stream.ReadAsync(headerBuffer, 0, RawEventHeader.HEADER_SIZE, cancellationToken)
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
        var stream = GetAggregateWriteStream(aggregateId);

        await WriteAsyncCore(events, stream, cancellationToken).ConfigureAwait(false);        
    }

    private async Task WriteAsyncCore(IEnumerable<Event> events, Stream stream, CancellationToken cancellationToken)
    {
        var eventsCount = events.Count();

        long maxPayloadSize = int.MinValue;
        for (int i = 0; i < eventsCount; i++)
        {
            var @event = events.ElementAt(i);

            if (!_eventTypes.TryGetValue(@event.Type, out SerializedString value))
            {
                var typeBytes = Encoding.UTF8.GetBytes(@event.Type);
                value = new SerializedString()
                {
                    Bytes = typeBytes,
                    Length = typeBytes.Length
                };
                _eventTypes.Add(@event.Type, value);
            }

            long tmp = @event.Data.LongLength + value.Length + _baseHeaderLength;
            if (maxPayloadSize < tmp)
                maxPayloadSize = tmp;
        }

        for (int i = 0; i < eventsCount; i++)
        {
            var @event = events.ElementAt(i);
            var eventType = _eventTypes[@event.Type];

            var header = new RawEventHeader
            {
                EventIndex = @event.Index,
                EventTypeNameLength = eventType.Length,
                EventDataLength = @event.Data.Length,
                Version = 1
            };

            await header.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);

            await stream.WriteAsync(eventType.Bytes, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(@event.Data, cancellationToken).ConfigureAwait(false);
        }

        await stream.FlushAsync().ConfigureAwait(false);
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
                    kv.Value.Close();
                    kv.Value.Dispose();
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
