using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("EvenireDB.Benchmark")]
[assembly: InternalsVisibleTo("EvenireDB.Tests")]

public record FileEventsRepositoryConfig(string BasePath, int MaxPageSize = 100);

public class FileEventsRepository : IDisposable, IEventsRepository
{
    private bool _disposedValue = false;
    
    private readonly Dictionary<Guid, Stream> _aggregateWriteStreams = new();
    private readonly Dictionary<string, byte[]> _eventTypes = new();
    private readonly FileEventsRepositoryConfig _config;

    public FileEventsRepository(FileEventsRepositoryConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (!Directory.Exists(_config.BasePath))
            Directory.CreateDirectory(config.BasePath);
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
    => Path.Combine(_config.BasePath, aggregateId.ToString() + ".dat");

    public async Task<IEnumerable<Event>> ReadAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        string aggregatePath = GetAggregateFilePath(aggregateId);
        if (!File.Exists(aggregatePath))
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

        var eventsCount = events.Count();

        var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.HEADER_SIZE);

        for (int i = 0; i < eventsCount; i++)
        {
            var @event = events.ElementAt(i);

            if (!_eventTypes.TryGetValue(@event.Type, out byte[] eventType))
            {
                eventType = Encoding.UTF8.GetBytes(@event.Type);
                _eventTypes.Add(@event.Type, eventType);
            }

            var header = new RawEventHeader
            {
                EventIndex = @event.Index,
                EventTypeNameLength = eventType.Length,
                EventDataLength = @event.Data.Length,
                Version = 1
            };
            header.Fill(headerBuffer);
            await stream.WriteAsync(headerBuffer, 0, RawEventHeader.HEADER_SIZE, cancellationToken)
                        .ConfigureAwait(false);

            await stream.WriteAsync(eventType, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(@event.Data, cancellationToken).ConfigureAwait(false);
        }

        ArrayPool<byte>.Shared.Return(headerBuffer);

        await stream.FlushAsync().ConfigureAwait(false);
    }

    //TODO: benchmark vs WriteAsync
    public async Task WriteSingleBufferAsync(Guid aggregateId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
        var stream = GetAggregateWriteStream(aggregateId);

        var eventsCount = events.Count();

        int maxPayloadSize = int.MinValue;
        for (int i = 0; i < eventsCount; i++)
        {
            var @event = events.ElementAt(i);

            if (!_eventTypes.TryGetValue(@event.Type, out byte[] eventType))
            {
                eventType = Encoding.UTF8.GetBytes(@event.Type);
                _eventTypes.Add(@event.Type, eventType);
            }

            int tmp = @event.Data.Length + eventType.Length + RawEventHeader.HEADER_SIZE;
            if (maxPayloadSize < tmp)
                maxPayloadSize = tmp;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(maxPayloadSize);

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
            header.Fill(buffer);

            Array.Copy(eventType, 0, buffer, RawEventHeader.HEADER_SIZE, eventType.Length);
            Array.Copy(@event.Data, 0, buffer, RawEventHeader.HEADER_SIZE + eventType.Length, @event.Data.Length);

            var bytesToWrite = RawEventHeader.HEADER_SIZE + eventType.Length + @event.Data.Length;
            await stream.WriteAsync(buffer, 0, bytesToWrite, cancellationToken)
                        .ConfigureAwait(false);
        }

        ArrayPool<byte>.Shared.Return(buffer);

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
