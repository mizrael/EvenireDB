using EvenireDB.Server;
using EvenireDB.Server.DTO;
using System.Threading.Channels;

public class IncomingEventsSubscriber : BackgroundService
{
    private readonly ChannelReader<IncomingEventsGroup> _reader;
    private readonly IEventsRepository _repo;

    public IncomingEventsSubscriber(ChannelReader<IncomingEventsGroup> reader, IEventsRepository repo)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Factory.StartNew(async () =>
        {
            await this.ExecuteAsyncCore(cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    private async Task ExecuteAsyncCore(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested || await _reader.WaitToReadAsync(cancellationToken))
        {
            while (_reader.TryRead(out IncomingEventsGroup group))
            {
                await _repo.WriteAsync(group.AggregateId, group.Events, cancellationToken)
                           .ConfigureAwait(false);
            }
        }
    }
}