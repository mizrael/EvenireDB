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

        private async Task<(List<RawEventHeader>, int)> ReadHeadersAsync(
            string headersPath, 
            Direction direction,
            int skip, 
            CancellationToken cancellationToken)
        {
            var headers = new List<RawEventHeader>(_config.MaxPageSize);

            using var headersStream = new FileStream(headersPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            headersStream.Position = RawEventHeader.SIZE * skip;

            int dataBufferSize = 0; // TODO: this should probably be a long, but it would then require chunking from the data stream

            // TODO: try reading a bigger buffer at once, and then parse the headers from it
            var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);

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

            return (headers, dataBufferSize);
        }

        public async ValueTask<IEnumerable<IEvent>> ReadAsync(Guid streamId, Direction direction = Direction.Forward, int skip = 0, CancellationToken cancellationToken = default)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            string headersPath = GetStreamPath(streamId, HeadersFileSuffix);
            string dataPath = GetStreamPath(streamId, DataFileSuffix);
            if (!File.Exists(headersPath) || !File.Exists(dataPath))
                return Array.Empty<Event>();

            // first, read all the headers, which are stored sequentially
            // this will allow later on pulling the data for all the events in one go

            var (headers, dataBufferSize) = await this.ReadHeadersAsync(headersPath, direction, skip, cancellationToken)
                                                      .ConfigureAwait(false);

            // we exit early if no headers found
            if (headers.Count == 0)
                return Array.Empty<Event>();

            // now we read the data for all the events in a single buffer
            // so that we can parse it directly, avoiding accessing the file any longer

            var results = new List<IEvent>();
            using var dataStream = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

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

                var @event = _factory.Create(headers[i].EventId, eventTypeName, eventData);
                results.Add(@event);
            }

            ArrayPool<byte>.Shared.Return(dataBuffer);

            return results;
        }

        public async ValueTask WriteAsync(Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            string dataPath = GetStreamPath(streamId, DataFileSuffix);
            using var dataStream = new FileStream(dataPath, FileMode.Append, FileAccess.Write, FileShare.Read);

            string headersPath = GetStreamPath(streamId, HeadersFileSuffix);
            using var headersStream = new FileStream(headersPath, FileMode.Append, FileAccess.Write, FileShare.Read);

            var eventsCount = events.Count();

            var headerBuffer = ArrayPool<byte>.Shared.Rent(RawEventHeader.SIZE);

            for (int i = 0; i < eventsCount; i++)
            {
                var @event = events.ElementAt(i);

                var eventType = _eventTypes.GetOrAdd(@event.Type, _ =>
                {
                    var src = Encoding.UTF8.GetBytes(@event.Type);
                    var dest = new byte[Constants.MAX_EVENT_TYPE_LENGTH];
                    Array.Copy(src, dest, src.Length);
                    return dest;
                });

                var header = new RawEventHeader
                {
                    EventId = @event.Id,
                    EventType = eventType,
                    DataPosition = dataStream.Position,
                    EventDataLength = @event.Data.Length,
                    EventTypeLength = (short)@event.Type.Length,
                };
                header.CopyTo(headerBuffer);
                await headersStream.WriteAsync(headerBuffer, 0, RawEventHeader.SIZE, cancellationToken)
                                     .ConfigureAwait(false);

                await dataStream.WriteAsync(@event.Data, cancellationToken).ConfigureAwait(false);
            }

            ArrayPool<byte>.Shared.Return(headerBuffer);

            await dataStream.FlushAsync().ConfigureAwait(false);
            await headersStream.FlushAsync().ConfigureAwait(false);
        }
    }
}