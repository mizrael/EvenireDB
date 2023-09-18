using EvenireDB.Server;
using EvenireDB.Server.Routes;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();

var channel = Channel.CreateUnbounded<IncomingEventsGroup>(new UnboundedChannelOptions
{
    SingleWriter = false,
    SingleReader = false,
    AllowSynchronousContinuations = true
});

builder.Services
    .AddSingleton(channel.Writer)
    .AddSingleton(channel.Reader)
    .AddSingleton(_ =>
    {
        // TODO: from config. default to this when config is empty
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        return new FileEventsRepositoryConfig(dataPath);
    })
    .AddSingleton<IEventsRepository, FileEventsRepository>()
    .AddHostedService<IncomingEventsSubscriber>();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/", () => "EvenireDB Server is running!");
app.MapEventsRoutes();

app.Run();

public partial class Program
{ }
