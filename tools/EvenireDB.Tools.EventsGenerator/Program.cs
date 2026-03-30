using EvenireDB.Client;
using EvenireDB.Client.Exceptions;
using EvenireDB.Common;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Text;

var defaultServerUri = new Uri("http://localhost");
var defaultEventsCount = 100;
var defaultGrpcPort = 5243;
var defaultHttpPort = 5001;

var serverOption = new Option<Uri>(name: "--server")
{
    Description = $"The server uri. Defaults to {defaultServerUri}.",
    DefaultValueFactory = _ => defaultServerUri
};

var useGrpcOption = new Option<bool>(name: "--useGrpc")
{
    Description = "Use gRPC instead of HTTP. Defaults to false."
};

var grpcPortOption = new Option<int>(name: "--grpcPort")
{
    Description = $"HTTP Port. Defaults to {defaultGrpcPort}.",
    DefaultValueFactory = _ => defaultGrpcPort
};

var httpPortOption = new Option<int>(name: "--httpPort")
{
    Description = $"HTTP Port. Defaults to {defaultHttpPort}.",
    DefaultValueFactory = _ => defaultHttpPort
};

var eventsCountOption = new Option<int>(name: "--count")
{
    Description = $"Count of events to send. Defaults to {defaultEventsCount}.",
    DefaultValueFactory = _ => defaultEventsCount
};

var streamIdOption = new Option<Guid>(name: "--stream")
{
    Description = "The stream to use."
};

var streamTypeOption = new Option<string>(name: "--type")
{
    Description = "The stream type.",
};

var rootCommand = new RootCommand
{
    serverOption,
    useGrpcOption,
    grpcPortOption,
    httpPortOption,
    eventsCountOption,
    streamIdOption,
    streamTypeOption
};
rootCommand.SetAction( async parseResult => {
    var uri = parseResult.GetValue(serverOption) ?? defaultServerUri;
    var useGrpc = parseResult.GetValue(useGrpcOption);
    var grpcPort = parseResult.GetValue(grpcPortOption);
    var httpPort = parseResult.GetValue(httpPortOption);
    var streamKey = parseResult.GetValue(streamIdOption);
    var streamType = parseResult.GetValue(streamTypeOption);
    var eventsCount = parseResult.GetValue(eventsCountOption);

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

    var streamId = new StreamId(streamKey, streamType);

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
});

await rootCommand.Parse(args).InvokeAsync();