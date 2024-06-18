using EvenireDB.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EvenireDB.Server
{
    public class IncomingEventsPersistenceWorker : BackgroundService
    {
        private readonly ChannelReader<IncomingEventsBatch> _reader;
        private readonly IEventsProvider _repo;
        private readonly ILogger<IncomingEventsPersistenceWorker> _logger;

        public IncomingEventsPersistenceWorker(
            ChannelReader<IncomingEventsBatch> reader, 
            IEventsProvider repo, 
            ILogger<IncomingEventsPersistenceWorker> logger)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(async () =>
            {
                await ExecuteAsyncCore(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        private async Task ExecuteAsyncCore(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested || await _reader.WaitToReadAsync(cancellationToken))
            {
                while (_reader.TryRead(out IncomingEventsBatch? batch) && batch is not null)
                {
                    try
                    {
                        await _repo.AppendAsync(batch.StreamId, batch.Events, cancellationToken)
                                   .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.EventsGroupPersistenceError(batch.StreamId, ex.Message);
                    }
                }
            }
        }
    }
}