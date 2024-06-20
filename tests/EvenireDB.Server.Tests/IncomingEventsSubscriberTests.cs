using EvenireDB.Common;
using EvenireDB.Persistence;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EvenireDB.Server.Tests;

public class IncomingEventsSubscriberTests
{
    [Fact]
    public async Task Service_should_handle_exceptions_gracefully()
    {
        var events = Array.Empty<Event>();
        var streamId = new StreamId(Guid.NewGuid(), "lorem");
        var expectedGroups = Enumerable.Range(0, 10)
            .Select(i => new IncomingEventsBatch(streamId, events))
            .ToArray();

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        int batchIndex = 0;

        var reader = Substitute.ForPartsOf<ChannelReader<IncomingEventsBatch>>();
        reader.WaitToReadAsync(Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(ValueTask.FromResult(batchIndex < expectedGroups.Length));
        reader.TryRead(out Arg.Any<IncomingEventsBatch>())
            .Returns(x =>
            {
                if (batchIndex >= expectedGroups.Length)
                {
                    cts.Cancel();
                    return false;
                }

                x[0] = expectedGroups[batchIndex++];
                return true;
            });

        var repo = Substitute.For<IEventsProvider>();
        repo.WhenForAnyArgs(r => r.AppendAsync(streamId, events, Arg.Any<CancellationToken>()))
            .Throw<Exception>();

        var logger = Substitute.For<ILogger<IncomingEventsPersistenceWorker>>();

        var sut = new IncomingEventsPersistenceWorker(reader, repo, logger);        
        
        await sut.StartAsync(cts.Token);

        await Task.Delay(TimeSpan.FromSeconds(2));
        
        await repo.ReceivedWithAnyArgs(expectedGroups.Length)
                  .AppendAsync(Arg.Any<StreamId>(), Arg.Any<IEnumerable<Event>>(), default);
    }
}