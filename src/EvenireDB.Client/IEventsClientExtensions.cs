﻿using EvenireDB.Common;
using System.Runtime.CompilerServices;

namespace EvenireDB.Client;

public static class IEventsClientExtensions
{
    public static IAsyncEnumerable<Event> ReadAsync(
        this IEventsClient client, 
        StreamId streamId,
        Direction direction = Direction.Forward, 
        CancellationToken cancellationToken = default)
        => client.ReadAsync(streamId, StreamPosition.Start, direction, cancellationToken);

    public static async IAsyncEnumerable<Event> ReadAllAsync(
        this IEventsClient client,
        StreamId streamId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        uint position = 0;

        while (true)
        {
            bool hasItems = false;
            await foreach (var item in client.ReadAsync(streamId, position: position, direction: Direction.Forward, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                position++;
                hasItems = true;
                yield return item;
            }

            if (!hasItems)
                break;
        }
    }
}