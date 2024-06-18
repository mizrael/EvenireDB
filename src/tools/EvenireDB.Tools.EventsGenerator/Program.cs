using EvenireDB.Client;
using EvenireDB.Client.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Text;

var defaultServerUri = new Uri("http://localhost");
var defaultEventsCount = 100;
var defaultGrpcPort = 5243;
var defaultHttpPort = 5001;

var serverOption = new Option<Uri>(
           name: "--server",
           getDefaultValue: () => defaultServerUri,
           description: $"The server uri. Defaults to {defaultServerUri}.");
var useGrpc = new Option<bool>(
              name: "--useGrpc",
              description: "Use gRPC instead of HTTP. Defaults to false.");
var grpcPort = new Option<int>(
              name: "--grpcPort",
              getDefaultValue: () => defaultGrpcPort,
              description: $"HTTP Port. Defaults to {defaultGrpcPort}.");
var httpPort = new Option<int>(
              name: "--httpPort",
              getDefaultValue: () => defaultHttpPort,
              description: $"HTTP Port. Defaults to {defaultHttpPort}.");
var eventsCount = new Option<int>(
              name: "--count",
              getDefaultValue: () => defaultEventsCount,
              description: $"Count of events to send. Defaults to {defaultEventsCount}.");

var streamIdOption = new Option<Guid>(
              name: "--stream",
              description: "The stream to use.");
var streamTypeOption = new Option<string>(
              name: "--type",
              description: "The stream type to use.");

var rootCommand = new RootCommand
{
    serverOption,
    useGrpc,
    grpcPort,
    httpPort,
    eventsCount,
    streamIdOption,
    streamTypeOption
};
rootCommand.SetHandler(async (streamId, streamType, uri, useGrpc, grpcPort, httpPort, eventsCount) => {
    var clientConfig = new EvenireClientConfig()
    {
        ServerUri = uri,
        UseGrpc = useGrpc,
        GrpcSettings = new EvenireClientConfig.GrpcTransportSettings(grpcPort),
        HttpSettings = new EvenireClientConfig.HttpTransportSettings(httpPort)
    };

    var services = new ServiceCollection();
    services.AddEvenireDB(clientConfig);

    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<IEventsClient>();

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Sending {eventsCount} events to stream '{streamType}/{streamId}' on server '{uri}'...");

    var events = Enumerable.Range(0, eventsCount).Select(i => new EventData($"event-{i}", Encoding.UTF8.GetBytes($"event-{i}"))).ToArray();

    try
    {
        await client.AppendAsync(streamId, streamType, events);
    }
    catch (ClientException cliEx)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("an error has occurred while sending events: " + cliEx.Message);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Done.");

    Console.ResetColor();
}, streamIdOption, streamTypeOption, serverOption, useGrpc, grpcPort, httpPort, eventsCount);

await rootCommand.InvokeAsync(args);