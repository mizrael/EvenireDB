var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();

builder.Services
    .AddSingleton(() =>
    {
        // TODO: from config. default to this when config is empty
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        return new FileEventsRepositoryConfig(dataPath);
    })
    .AddSingleton<IEventsRepository, FileEventsRepository>();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/", () => "EvenireDB Server is running!");
app.MapEventsRoutes();

app.Run();
