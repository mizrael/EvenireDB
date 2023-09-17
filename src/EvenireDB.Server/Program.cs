using EvenireDB.Server.Routes;
using EvenireDB.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();

builder.Services
    .AddSingleton<FileEventsRepositoryConfig>(_ =>
    {
        // TODO: from config. default to this when config is empty
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        return new FileEventsRepositoryConfig(dataPath);
    })    
    .AddSingleton<IEventsRepository, FileEventsRepository>()
    .AddSingleton(EventsProcessorConfig.Default)
    .AddSingleton<IEventsProcessor, EventsProcessor>();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/", () => "EvenireDB Server is running!");
app.MapEventsRoutes();

app.Run();

public partial class Program
{ }