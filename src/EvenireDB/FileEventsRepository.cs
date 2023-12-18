using EvenireDB.Common;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace EvenireDB
{
    public class FileEventsRepository : IEventsRepository
    {
        private readonly ConcurrentDictionary<string, byte[]> _eventTypes = new();
        private readonly FileEventsRepositoryConfig _config;
        private readonly IEventDataValidator _factory;

        private const string DataFileSuffix = "_data";
        private const string HeadersFileSuffix = "_headers";

        public FileEventsRepository(FileEventsRepositoryConfig config, IEventDataValidator factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (!Directory.Exists(_config.BasePath))
                Directory.CreateDirectory(config.BasePath);
        }

        private string GetStreamPath(Guid streamId, string type)
        => Path.Combine(_config.BasePath, $"{streamId}{type}.dat");

        public async IAsyncEnumerable<Event> ReadAsync(
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
            var headersBufferMem = headersBuffer.AsMemory(0, headerBufferLen);

            try
            {
                while (true)
                {
                    int bytesRead = await headersStream.ReadAsync(headersBufferMem, cancellationToken)
                                                       .ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;

                    int offset = 0;
                    while (offset < bytesRead)
                    {
                        var header = new RawEventHeader(headersBufferMem.Slice(offset, RawEventHeader.SIZE));
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
            var dataBufferMem = dataBuffer.AsMemory(0, dataBufferSize);
            
            try
            {
                await dataStream.ReadAsync(dataBufferMem, cancellationToken)
                                .ConfigureAwait(false);

                var encoding = Encoding.UTF8;
                for (int i = 0; i < headers.Count; i++)
                {
                    //TODO: optimize ?
                    var eventTypeName = encoding.GetString(headers[i].EventType, 0, headers[i].EventTypeLength);

                    var destEventData = new byte[headers[i].EventDataLength];

                    // if skip is specified, when calculating the source offset we need to subtract the position of the first block of data
                    // because the stream position was already set after opening it
                    long srcOffset = headers[i].DataPosition - headers[0].DataPosition;
                    
                    // need to copy the memory here as the source array is rented 
                    // AND we need to give the event data to the calling client
                    dataBufferMem.Slice((int)srcOffset, headers[i].EventDataLength)
                                 .CopyTo(destEventData);

                    var eventId = new EventId(headers[i].EventIdTimestamp, headers[i].EventIdSequence);
                    var @event = new Event(eventId, eventTypeName, destEventData);
                    yield return @event;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(dataBuffer);
            }            
        }

        public async ValueTask AppendAsync(Guid streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
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

                    var eventType = _eventTypes.GetOrAdd(@event.Type, static type =>
                    {
                        var dest = new byte[Constants.MAX_EVENT_TYPE_LENGTH]; 
                        Encoding.UTF8.GetBytes(type, dest);
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