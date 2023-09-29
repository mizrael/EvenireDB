using EvenireDB;
using System.Threading.Channels;

public class IncomingEventsPersistenceWorker : BackgroundService
{
    private readonly ChannelReader<IncomingEventsGroup> _reader;
    private readonly IEventsRepository _repo;
    private readonly ILogger<IncomingEventsPersistenceWorker> _logger;

    public IncomingEventsPersistenceWorker(ChannelReader<IncomingEventsGroup> reader, IEventsRepository repo, ILogger<IncomingEventsPersistenceWorker> logger)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Factory.StartNew(async () =>
        {
            // TODO: handle exceptions, otherwise the reader will fail
            await this.ExecuteAsyncCore(cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    private async Task ExecuteAsyncCore(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested || await _reader.WaitToReadAsync(cancellationToken))
        {
            while (_reader.TryRead(out IncomingEventsGroup? group) && group is not null)
            {
                try
                {
                    await _repo.AppendAsync(group.AggregateId, group.Events, cancellationToken)
                           .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "an error has occurred while persisting events group for aggregate {AggregateId}: {Error}",
                        group.AggregateId,
                        ex.Message);
                }
                
            }
        }
    }
}