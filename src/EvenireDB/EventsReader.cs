﻿using EvenireDB.Common;
using EvenireDB.Persistence;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace EvenireDB
{
    internal class EventsReader : IEventsReader
    {
        private readonly IEventsCache _cache;
        private readonly EventsReaderConfig _config;
        
        public EventsReader(
            EventsReaderConfig config,            
            IEventsCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async IAsyncEnumerable<Event> ReadAsync(
            Guid streamId,
            StreamPosition startPosition,
            Direction direction = Direction.Forward,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (startPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(startPosition));

            CachedEvents entry = await _cache.EnsureStreamAsync(streamId, cancellationToken).ConfigureAwait(false);

            if (entry?.Events == null || entry.Events.Count == 0)
                yield break;

            uint totalCount = (uint)entry.Events.Count;
            uint pos = startPosition;

            if (direction == Direction.Forward)
            {
                if (totalCount < startPosition)
                    yield break;

                uint j = 0, i = pos,
                    finalCount = Math.Min(_config.MaxPageSize, totalCount - i);

                while (j++ != finalCount)
                {
                    yield return entry.Events[(int)i++];
                }
            }
            else
            {
                if (startPosition == StreamPosition.End)
                    pos = totalCount - 1;

                if (pos >= totalCount)
                    yield break;

                uint j = 0, i = pos,
                      finalCount = Math.Min(_config.MaxPageSize, i + 1);

                while (j++ != finalCount)
                {
                    yield return entry.Events[(int)i--];
                }
            }
        }
    }
}