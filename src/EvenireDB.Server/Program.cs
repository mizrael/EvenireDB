using EvenireDB;
using EvenireDB.Server.Routes;
using EvenireDB.Utils;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
            ctx.ProblemDetails.Extensions.Add("nodeId", Environment.MachineName));

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    var serverConfig = context.Configuration.GetRequiredSection("Server").Get<ServerConfig>();

    options.ListenAnyIP(serverConfig.GrpcPort, o => o.Protocols = HttpProtocols.Http2);
    options.ListenAnyIP(serverConfig.HttpPort, o => o.Protocols = HttpProtocols.Http1);
});

builder.Services.AddGrpc();

var channel = Channel.CreateUnbounded<IncomingEventsGroup>(new UnboundedChannelOptions
{
    SingleWriter = false,
    SingleReader = false,
    AllowSynchronousContinuations = true
});

builder.Services
    .Configure<ServerConfig>(builder.Configuration.GetSection("Server"))
    .AddSingleton<ICache<Guid, CachedEvents>>(ctx =>
    {
        var serverConfig = ctx.GetRequiredService<IOptions<ServerConfig>>().Value;
        return new LRUCache<Guid, CachedEvents>(serverConfig.CacheCapacity);
    })
    .AddSingleton(ctx =>
    {
        var serverConfig = ctx.GetRequiredService<IOptions<ServerConfig>>().Value;
        return new EventsProviderConfig(serverConfig.MaxPageSizeToClient);
    })
    .AddSingleton<EventsProvider>()
    .AddSingleton(channel.Writer)
    .AddSingleton(channel.Reader)
    .AddSingleton<IEventFactory>(ctx =>
    {
        var serverConfig = ctx.GetRequiredService<IOptions<ServerConfig>>().Value;
        return new EventFactory(serverConfig.MaxEventDataSize);
    })
    .AddSingleton<EventMapper>()
    .AddSingleton(ctx =>
    {
        var serverConfig = ctx.GetRequiredService<IOptions<ServerConfig>>().Value;

        var dataPath = serverConfig.DataFolder;
        if (string.IsNullOrWhiteSpace(dataPath))
            dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        else
        {
            if (!Path.IsPathFullyQualified(dataPath))
                dataPath = Path.Combine(AppContext.BaseDirectory, dataPath);
        }

        return new FileEventsRepositoryConfig(dataPath, serverConfig.MaxEventsPageSizeFromDisk);
    })
    .AddSingleton<IEventsRepository, FileEventsRepository>()
    .AddHostedService<IncomingEventsPersistenceWorker>();

var version = Assembly.GetExecutingAssembly().GetName().Version;

var app = builder.Build();
app.UseExceptionHandler(exceptionHandlerApp
    => exceptionHandlerApp.Run(async context => await Results.Problem().ExecuteAsync(context)));
app.MapHealthChecks("/healthz");
app.MapGet("/", () => $"EvenireDB Server v{version} is running!");

app.MapEventsRoutes();
app.MapGrpcService<EvenireDB.Server.Grpc.EventsService>();

app.Run();

public partial class Program
{ }
