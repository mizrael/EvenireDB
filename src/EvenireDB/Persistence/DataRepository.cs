using EvenireDB.Common;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace EvenireDB.Persistence;

internal class DataRepository : IDataRepository
{
    private readonly ConcurrentDictionary<string, byte[]> _eventTypes = new();
    private readonly int _bufferSize;

    public DataRepository(int bufferSize = 262144)
    {
        _bufferSize = bufferSize;
    }

    public async IAsyncEnumerable<Event> ReadAsync(
        ExtentInfo extentInfo,
        IAsyncEnumerable<RawHeader> headers,
        CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(extentInfo.DataPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                                          bufferSize: _bufferSize, useAsync: true);
        
        var typeBuffer = ArrayPool<byte>.Shared.Rent(Constants.MAX_EVENT_TYPE_LENGTH);

        try
        {
            await foreach (var header in headers)
            {
                stream.Position = header.DataOffset;

                var typeMemory = typeBuffer.AsMemory(0, Constants.MAX_EVENT_TYPE_LENGTH);
                await stream.ReadExactlyAsync(typeMemory, cancellationToken);
                var eventType = Encoding.UTF8.GetString(typeBuffer, 0, header.TypeLength);

                var destEventData = new byte[header.DataLength];
                await stream.ReadExactlyAsync(destEventData, 0, header.DataLength, cancellationToken);

                var eventId = new EventId(header.IdTimestamp, header.IdSequence);
                var @event = new Event(eventId, eventType, destEventData);
                yield return @event;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(typeBuffer);
        }
    }

    public async IAsyncEnumerable<RawHeader> AppendAsync(
        ExtentInfo extentInfo,
        IEnumerable<Event> events,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var eventsCount = events.Count();

        using var stream = new FileStream(extentInfo.DataPath, FileMode.Append, FileAccess.Write,
                                          FileShare.Read, bufferSize: 4096, useAsync: true);

        for (int i = 0; i < eventsCount; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var @event = events.ElementAt(i);

            var eventType = _eventTypes.GetOrAdd(@event.Type, static type =>
            {
                var dest = new byte[Constants.MAX_EVENT_TYPE_LENGTH];
                Encoding.UTF8.GetBytes(type, dest);
                return dest;
            });

            var header = new RawHeader(
                    idTimestamp: @event.Id.Timestamp,
                    idSequence: @event.Id.Sequence,
                    typeLen: (short)@event.Type.Length,
                    dataOffset: stream.Position,
                    dataLen: @event.Data.Length
                );

            await stream.WriteAsync(eventType, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(@event.Data, cancellationToken).ConfigureAwait(false);

            yield return header;
        }

        await stream.FlushAsync().ConfigureAwait(false);
    }
}