using EvenireDB;
using EvenireDB.Client;
using EvenireDB.Common;
using EvenireDB.Samples.TemperatureSensors;
using EvenireDB.Server.Routes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

var serverSettings = builder.Configuration.GetSection("Evenire").Get<EvenireServerSettings>()!;
var sensorSettings = builder.Configuration.GetSection("Settings").Get<Settings>()!;

// Configure Kestrel with dual ports: HTTP (for REST + sample) and gRPC
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.ListenLocalhost(serverSettings.HttpSettings.Port, o => o.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenLocalhost(serverSettings.GrpcSettings.Port, o => o.Protocols = HttpProtocols.Http2);
});

// Register EvenireDB server engine
builder.Services.Configure<EvenireServerSettings>(builder.Configuration.GetSection("Evenire"));
builder.Services.AddEvenire();

// Register gRPC
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();
builder.Services.AddSingleton<EventMapper>();

// Register EvenireDB client (connects to the same process via localhost)
var clientConfig = new EvenireClientConfig
{
    ServerUri = new Uri($"http://localhost"),
    UseGrpc = false,
    HttpSettings = new EvenireClientConfig.HttpTransportSettings(serverSettings.HttpSettings.Port)
};
builder.Services.AddEvenireDB(clientConfig);

// Register sample services
builder.Services.AddSingleton(sensorSettings);
builder.Services.AddHostedService<SensorsFakeProducer>();

var app = builder.Build();

// Map EvenireDB server endpoints
app.MapGrpcService<EvenireDB.Server.Grpc.EventsGrcpServiceImpl>();
app.MapGrpcHealthChecksService();
app.MapHealthChecks("/healthz");
app.MapEventsRoutes();

// Map sample sensor endpoints
app.MapGet("/sensors", ([FromServices] Settings config) =>
{
    return config.SensorIds.Select(id => new
    {
        id,
        links = new
        {
            details = $"/sensors/{id}"
        }
    });
});

app.MapGet("/sensors/{sensorId:guid}", async ([FromServices] IEventsClient client, Guid sensorId) =>
{
    var events = new List<EvenireDB.Client.Event>();
    var streamId = new StreamId(sensorId, nameof(Sensor));
    await foreach (var item in client.ReadAsync(streamId, StreamPosition.End, Direction.Backward).ConfigureAwait(false))
        events.Add(item);

    if (events.Count == 0)
        return Results.NotFound();

    var sensor = Sensor.Create(sensorId, events);
    return Results.Ok(sensor);
});

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\nTemperature Sensors sample is starting...");
Console.WriteLine($"  EvenireDB HTTP: http://localhost:{serverSettings.HttpSettings.Port}");
Console.WriteLine($"  Sensors API:    http://localhost:{serverSettings.HttpSettings.Port}/sensors");
Console.ResetColor();

app.Run();
