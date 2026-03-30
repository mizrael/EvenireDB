using EvenireDB.Common;
using System.Runtime.CompilerServices;

namespace EvenireDB;

internal class EventsReader : IEventsReader
{
    private readonly IStreamsCache _cache;
    private readonly EventsReaderConfig _config;
    
    public EventsReader(
        EventsReaderConfig config,            
        IStreamsCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async IAsyncEnumerable<Event> ReadAsync(
        StreamId streamId,        
        StreamPosition startPosition,
        Direction direction = Direction.Forward,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {        
        CachedEvents entry = await _cache.GetEventsAsync(streamId, cancellationToken).ConfigureAwait(false);

        if (entry?.Events == null || entry.Events.Count == 0)
            yield break;

        int totalCount = entry.Events.Count;

        if (direction == Direction.Forward)
        {
            if (totalCount < (uint)startPosition)
                yield break;

            int i = (int)(uint)startPosition;
            int finalCount = Math.Min(_config.MaxPageSize, totalCount - i);

            for (int j = 0; j < finalCount; j++)
                yield return entry.Events[i++];
        }
        else
        {
            int pos = startPosition == StreamPosition.End
                ? totalCount - 1
                : (int)(uint)startPosition;

            if (pos >= totalCount)
                yield break;

            int finalCount = Math.Min(_config.MaxPageSize, pos + 1);

            for (int j = 0; j < finalCount; j++)
                yield return entry.Events[pos--];
        }
    }
}