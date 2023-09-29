using System.Runtime.CompilerServices;

namespace EvenireDB.Client
{
    public static class IEventsClientExtensions
    {
        public static Task<IEnumerable<Event>> ReadAsync(this IEventsClient client, Guid streamId, Direction direction = Direction.Forward, CancellationToken cancellationToken = default)
            => client.ReadAsync(streamId, StreamPosition.Start, direction, cancellationToken);

        public static async IAsyncEnumerable<Event> ReadAllAsync(
            this IEventsClient client,
            Guid streamId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            uint position = 0;
            while (true)
            {
                var currPage = await client.ReadAsync(streamId, position: position, direction: Direction.Forward, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (currPage is null || currPage.Count() == 0)
                    break;
                
                foreach(var @event in  currPage)
                    yield return @event;

                position += (uint)currPage.Count();
            }
        }
    }
}