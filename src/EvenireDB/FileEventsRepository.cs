using EvenireDB.Common;
using EvenireDB.Extents;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace EvenireDB
{
    internal class FileEventsRepository : IEventsRepository
    {
        private readonly FileEventsRepositoryConfig _config;
        private readonly ConcurrentDictionary<string, byte[]> _eventTypes = new();
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _streamLocks = new();
        private readonly IExtentInfoProvider _extentInfoProvider;

        public FileEventsRepository(
            FileEventsRepositoryConfig config,
            IExtentInfoProvider extentInfoProvider)
        {
            _extentInfoProvider = extentInfoProvider ?? throw new ArgumentNullException(nameof(extentInfoProvider));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async ValueTask<IEnumerable<Event>> ReadPageAsync(
            Guid streamId,
            int streamPosition,
            CancellationToken cancellationToken = default)
        {
            var extentInfo = _extentInfoProvider.GetLatest(streamId);
            if (!File.Exists(extentInfo.DataPath) || !File.Exists(extentInfo.HeadersPath))
                return Array.Empty<Event>();

            using var headersStream = new FileStream(extentInfo.HeadersPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var headers = new List<RawEventHeader>();
            int dataBufferSize = 0;

            int pageSize = RawEventHeader.SIZE * (int)_config.MaxPageSize;
            int headerBufferOffset = RawEventHeader.SIZE * streamPosition;

            int headerBufferLen = RawEventHeader.SIZE * (int)_config.MaxPageSize;
            var headersBuffer = ArrayPool<byte>.Shared.Rent(headerBufferLen);
            
            try
            {
                while (true)
                {
                    int bytesRead = await headersStream.ReadAsync(headersBuffer, offset: headerBufferOffset, count: pageSize, cancellationToken)
                                                       .ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;

                    int offset = 0;
                    while (offset < bytesRead)
                    {
                        var header = new RawEventHeader(headersBuffer.AsSpan(offset, RawEventHeader.SIZE));
                        headers.Add(header);

                        dataBufferSize += header.EventDataLength;
                        offset += RawEventHeader.SIZE;
                    }
                    headerBufferOffset += offset;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(headersBuffer);
            }

            if (headers.Count == 0)
                return Array.Empty<Event>();

            return null;
        }

        public async ValueTask<IAsyncEnumerable<Event>> ReadAsync(
            Guid streamId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var extentInfo = _extentInfoProvider.GetLatest(streamId);
            if (!File.Exists(extentInfo.DataPath) || !File.Exists(extentInfo.HeadersPath))
            
            //     yield break;

            var (headers, dataBufferSize) = await ReadHeadersAsync(extentInfo, 0, (int)_config.MaxPageSize, cancellationToken).ConfigureAwait(false);
            // if (headers.Count == 0)
            //     yield break;
            
            return ReadDataAsync(extentInfo, headers, dataBufferSize, cancellationToken);
        }

        private static async ValueTask<(List<RawEventHeader> headers, int dataBufferSize)> ReadHeadersAsync(
            ExtentInfo extentInfo,
            int streamVersion,
            int pageSize,
            CancellationToken cancellationToken)
        {
            using var headersStream = new FileStream(extentInfo.HeadersPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var headers = new List<RawEventHeader>();
            int dataBufferSize = 0;
            
            int headerBufferLen = RawEventHeader.SIZE * pageSize;
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

            return (headers, dataBufferSize);
        }

        private static async IAsyncEnumerable<Event> ReadDataAsync(
            ExtentInfo extentInfo, 
            IList<RawEventHeader> headers, 
            int dataBufferSize, 
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // now we read the data for all the events in a single buffer
            // so that we can parse it directly, avoiding accessing the file any longer
            var dataStream = new FileStream(extentInfo.DataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
            var extentInfo = _extentInfoProvider.GetLatest(streamId);

            var eventsCount = events.Count();

            using var dataStream = new FileStream(extentInfo.DataPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var headersStream = new FileStream(extentInfo.HeadersPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE * eventsCount);
            
            var semaphore = _streamLocks.GetOrAdd(streamId, static _ => new SemaphoreSlim(1, 1));
            semaphore.Wait(cancellationToken);

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
                    header.ToBytes(ref headerBuffer, offset: i * RawEventHeader.SIZE);

                    await dataStream.WriteAsync(@event.Data, cancellationToken).ConfigureAwait(false);
                }

                await headersStream.WriteAsync(headerBuffer, 0, RawEventHeader.SIZE * eventsCount, cancellationToken)
                                   .ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(headerBuffer);
                semaphore.Release();
            }

            await dataStream.FlushAsync().ConfigureAwait(false);
            await headersStream.FlushAsync().ConfigureAwait(false);
        }
    }
}