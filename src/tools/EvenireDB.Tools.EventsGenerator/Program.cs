using EvenireDB.Client;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Text;

var defaultServerUri = new Uri("http://localhost:5001");
var defaultEventsCount = 100;

var serverOption = new Option<Uri>(
           name: "--server",
           getDefaultValue: () => defaultServerUri,
           description: $"The server uri. Defaults to {defaultServerUri}.");
var useGrpc = new Option<bool>(
              name: "--grpc",
              description: "Use gRPC instead of HTTP. Defaults to false.");
var eventsCount = new Option<int>(
              name: "--count",
              getDefaultValue: () => defaultEventsCount,
              description: $"Count of events to send. Defaults to {defaultEventsCount}.");

var streamIdOption = new Option<Guid>(
              name: "--stream",
              description: "The stream to use.");


var rootCommand = new RootCommand
{
    serverOption,
    useGrpc,
    eventsCount,
    streamIdOption
};
rootCommand.SetHandler(async (streamId, uri, useGrpc, eventsCount) => {
    var services = new ServiceCollection();
    services.AddEvenireDB(new EvenireConfig(uri, useGrpc));

    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<IEventsClient>();

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Sending {eventsCount} events to stream '{streamId}' on server '{uri}'...");

    var events = Enumerable.Range(0, eventsCount).Select(i => new EventData($"event-{i}", Encoding.UTF8.GetBytes($"event-{i}"))).ToArray();

    try
    {
        await client.AppendAsync(streamId, events);
    }
    catch (ClientException cliEx)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("an error has occurred while sending events: " + cliEx.Message);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Done.");

    Console.ResetColor();
}, streamIdOption, serverOption, useGrpc, eventsCount);

await rootCommand.InvokeAsync(args);