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
        var channel = Channel.CreateBounded<IncomingEventsBatch>(10);
        var repo = Substitute.For<IEventsProvider>();
        repo.WhenForAnyArgs(r => r.AppendAsync(Arg.Any<StreamId>(), null, default))
            .Throw<Exception>();

        var logger = Substitute.For<ILogger<IncomingEventsPersistenceWorker>>();

        var sut = new IncomingEventsPersistenceWorker(channel.Reader, repo, logger);

        await sut.StartAsync(default);

        var groups = Enumerable.Range(0, 10)
            .Select(i => new IncomingEventsBatch(Arg.Any<StreamId>(), null))
            .ToArray();
        foreach (var group in groups)
            await channel.Writer.WriteAsync(group);

        await Task.Delay(2000);
        await sut.StopAsync(default);

        await repo.ReceivedWithAnyArgs(groups.Length)
                  .AppendAsync(Arg.Any<StreamId>(), null, default);
    }
}