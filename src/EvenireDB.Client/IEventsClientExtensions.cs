using EvenireDB.Common;
using System.Runtime.CompilerServices;

namespace EvenireDB.Client;

public static class IEventsClientExtensions
{
    public static IAsyncEnumerable<Event> ReadAsync(
        this IEventsClient client, 
        Guid streamId,
        string streamType,
        Direction direction = Direction.Forward, 
        CancellationToken cancellationToken = default)
        => client.ReadAsync(streamId, streamType, StreamPosition.Start, direction, cancellationToken);

    public static async IAsyncEnumerable<Event> ReadAllAsync(
        this IEventsClient client,
        Guid streamId,
        string streamType,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        uint position = 0;

        while (true)
        {
            bool hasItems = false;
            await foreach (var item in client.ReadAsync(streamId, streamType, position: position, direction: Direction.Forward, cancellationToken: cancellationToken).ConfigureAwait(false))
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