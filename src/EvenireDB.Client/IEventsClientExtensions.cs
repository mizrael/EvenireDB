using System.Runtime.CompilerServices;

namespace EvenireDB.Client
{
    public static class IEventsClientExtensions
    {
        public static async IAsyncEnumerable<Event> ReadAllAsync(
            this IEventsClient client,
            Guid streamId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var page = 0;
            while (true)
            {
                var currPage = await client.ReadAsync(streamId, page, cancellationToken).ConfigureAwait(false);
                if (currPage is null || currPage.Count() == 0)
                    break;
                
                foreach(var @event in  currPage)
                    yield return @event;

                page += currPage.Count();
            }
        }
    }
}