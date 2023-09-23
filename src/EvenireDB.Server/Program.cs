using EvenireDB;
using EvenireDB.Server.Routes;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
            ctx.ProblemDetails.Extensions.Add("nodeId", Environment.MachineName));

var channel = Channel.CreateUnbounded<IncomingEventsGroup>(new UnboundedChannelOptions
{
    SingleWriter = false,
    SingleReader = false,
    AllowSynchronousContinuations = true
});

builder.Services
    .AddMemoryCache()
    .AddSingleton(EventsProviderConfig.Default)
    .AddSingleton<EventsProvider>()
    .AddSingleton(channel.Writer)
    .AddSingleton(channel.Reader)
    .AddSingleton(_ =>
    {
        // TODO: from config. default to this when config is empty
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        return new FileEventsRepositoryConfig(dataPath);
    })
    .AddSingleton<IEventsRepository, FileEventsRepository>()
    .AddHostedService<IncomingEventsPersistenceWorker>();

var app = builder.Build();
app.UseExceptionHandler(exceptionHandlerApp
    => exceptionHandlerApp.Run(async context => await Results.Problem().ExecuteAsync(context)));
app.MapHealthChecks("/healthz");
app.MapGet("/", () => "EvenireDB Server is running!");
app.MapEventsRoutes();

app.Run();

public partial class Program
{ }
