﻿using EvenireDB.Common;

namespace EvenireDB;

public interface IEventsReader
{
    IAsyncEnumerable<Event> ReadAsync(
        Guid streamId,
        StreamPosition startPosition,
        Direction direction = Direction.Forward,
        CancellationToken cancellationToken = default);
}