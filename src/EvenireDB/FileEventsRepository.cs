using EvenireDB.Common;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("EvenireDB.Benchmark")]
[assembly: InternalsVisibleTo("EvenireDB.Tests")]

namespace EvenireDB
{
    public class FileEventsRepository : IEventsRepository
    {
        private readonly ConcurrentDictionary<string, byte[]> _eventTypes = new();
        private readonly FileEventsRepositoryConfig _config;
        private readonly IEventFactory _factory;

        private const string DataFileSuffix = "_data";
        private const string HeadersFileSuffix = "_headers";

        public FileEventsRepository(FileEventsRepositoryConfig config, IEventFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (!Directory.Exists(_config.BasePath))
                Directory.CreateDirectory(config.BasePath);
        }

        private string GetStreamPath(Guid streamId, string type)
        => Path.Combine(_config.BasePath, $"{streamId}{type}.dat");

        public async IAsyncEnumerable<IEvent> ReadAsync(
            Guid streamId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string headersPath = GetStreamPath(streamId, HeadersFileSuffix);
            string dataPath = GetStreamPath(streamId, DataFileSuffix);
            if (!File.Exists(headersPath) || !File.Exists(dataPath))
                yield break;

            using var headersStream = new FileStream(headersPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var headers = new List<RawEventHeader>();
            int dataBufferSize = 0;

            int headerBufferLen = RawEventHeader.SIZE * (int)_config.MaxPageSize;
            var headersBuffer = ArrayPool<byte>.Shared.Rent(headerBufferLen);

            try
            {
                while (true)
                {
                    int bytesRead = await headersStream.ReadAsync(headersBuffer, 0, headerBufferLen, cancellationToken)
                                                       .ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;

                    int offset = 0;
                    while (offset < bytesRead)
                    {
                        var header = new RawEventHeader(headersBuffer.AsMemory(offset, RawEventHeader.SIZE));
                        headers.Add(header);

                        dataBufferSize += header.EventDataLength;
                        offset += RawEventHeader.SIZE;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(headersBuffer);
            }

            // we exit early if no headers found
            if (headers.Count == 0)
                yield break;

            // now we read the data for all the events in a single buffer
            // so that we can parse it directly, avoiding accessing the file any longer
            using var dataStream = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            dataStream.Position = headers[0].DataPosition;

            var dataBuffer = ArrayPool<byte>.Shared.Rent(dataBufferSize);

            try
            {
                await dataStream.ReadAsync(dataBuffer, 0, dataBufferSize, cancellationToken)
                                .ConfigureAwait(false);

                for (int i = 0; i < headers.Count; i++)
                {
                    // have to create a span to ensure that we're parsing the right buffer size
                    // as ArrayPool might return a buffer with size the closest pow(2)
                    var eventTypeName = Encoding.UTF8.GetString(headers[i].EventType, 0, headers[i].EventTypeLength);

                    var eventData = new byte[headers[i].EventDataLength];

                    // if skip is specified, when calculating the source offset we need to subtract the position of the first block of data
                    // because the stream position was already set after opening it
                    long srcOffset = headers[i].DataPosition - headers[0].DataPosition;
                    Array.Copy(dataBuffer, srcOffset, eventData, 0, headers[i].EventDataLength);

                    var @event = _factory.Create(headers[i].EventId, eventTypeName, eventData);
                    yield return @event;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(dataBuffer);
            }            
        }

        public async ValueTask AppendAsync(Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            string dataPath = GetStreamPath(streamId, DataFileSuffix);
            using var dataStream = new FileStream(dataPath, FileMode.Append, FileAccess.Write, FileShare.Read);

            string headersPath = GetStreamPath(streamId, HeadersFileSuffix);
            using var headersStream = new FileStream(headersPath, FileMode.Append, FileAccess.Write, FileShare.Read);

            var eventsCount = events.Count();

            byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);

            try
            {
                for (int i = 0; i < eventsCount; i++)
                {
                    var @event = events.ElementAt(i);

                    var eventType = _eventTypes.GetOrAdd(@event.Type, _ =>
                    {
                        var dest = new byte[Constants.MAX_EVENT_TYPE_LENGTH]; 
                        Encoding.UTF8.GetBytes(@event.Type, dest);
                        return dest;
                    });

                    var header = new RawEventHeader(
                        eventId: @event.Id,
                        eventType: eventType,
                        dataPosition: dataStream.Position,
                        eventDataLength: @event.Data.Length,
                        eventTypeLength: (short)@event.Type.Length
                    );
                    header.ToBytes(ref headerBuffer);
                    await headersStream.WriteAsync(headerBuffer, 0, RawEventHeader.SIZE, cancellationToken)
                                       .ConfigureAwait(false);

                    await dataStream.WriteAsync(@event.Data, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(headerBuffer);
            }            

            await dataStream.FlushAsync().ConfigureAwait(false);
            await headersStream.FlushAsync().ConfigureAwait(false);
        }
    }
}