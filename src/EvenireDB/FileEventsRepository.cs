using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("EvenireDB.Benchmark")]
[assembly: InternalsVisibleTo("EvenireDB.Tests")]

public record FileEventsRepositoryConfig(string BasePath, int MaxPageSize = 100);

//TODO: add versioning on events file
public class FileEventsRepository : IDisposable, IEventsRepository
{
    private bool _disposedValue = false;
    
    private readonly ConcurrentDictionary<Guid, Stream> _aggregateWriteStreams = new();
    private readonly ConcurrentDictionary<string, byte[]> _eventTypes = new();
    private readonly FileEventsRepositoryConfig _config;

    public FileEventsRepository(FileEventsRepositoryConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (!Directory.Exists(_config.BasePath))
            Directory.CreateDirectory(config.BasePath);
    }

    private Stream GetAggregateWriteStream(Guid aggregateId)
    {
        var stream = _aggregateWriteStreams.GetOrAdd(aggregateId, _ =>
        {
            string aggregatePath = GetAggregateFilePath(aggregateId);
            return new FileStream(aggregatePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);           
        });

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

            var eventType = _eventTypes.GetOrAdd(@event.Type, _ => Encoding.UTF8.GetBytes(@event.Type));

            var header = new RawEventHeader
            {
                EventIndex = @event.Index,
                EventTypeNameLength = eventType.Length,
                EventDataLength = @event.Data.Length,
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
