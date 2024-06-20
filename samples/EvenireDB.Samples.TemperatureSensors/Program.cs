using EvenireDB.Client;
using EvenireDB.Common;
using EvenireDB.Samples.TemperatureSensors;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var settings = builder.Configuration.GetSection("Settings").Get<Settings>();

var clientConfig = builder.Configuration.GetSection("Evenire").Get<EvenireClientConfig>();

builder.Services.AddHostedService<SensorsFakeProducer>()
                .AddSingleton(settings)
                .AddEvenireDB(clientConfig);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/sensors", ([FromServices] Settings config) =>
{
    return config.SensorIds.Select(id => new
    {
        id,
        links = new { 
            details = $"/sensors/{id}"
        }
    });
});
app.MapGet("/sensors/{sensorId}", async ([FromServices] IEventsClient client, Guid sensorId) =>
{
    var events = new List<Event>();
    var streamId = new StreamId(sensorId, nameof(Sensor));
    await foreach(var item in client.ReadAsync(streamId, StreamPosition.End, Direction.Backward).ConfigureAwait(false))
        events.Add(item);

    if (events.Count() == 0)
        return Results.NotFound();

    var sensor = Sensor.Create(sensorId, events);
    return Results.Ok(sensor);
});

app.Run();
