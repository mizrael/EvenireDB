using EvenireDB.Client;
using EvenireDB.Common;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = new Uri(builder.Configuration.GetConnectionString("evenire"));

Settings settings = builder.Configuration.GetSection("Settings").Get<Settings>();

builder.Services.AddHostedService<SensorsFakeProducer>()
                .AddSingleton(settings)
                .AddEvenireDB(new EvenireConfig(connectionString, settings.UseGrpc));

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
    await foreach(var item in client.ReadAsync(sensorId, StreamPosition.End, Direction.Backward).ConfigureAwait(false))
        events.Add(item);

    if (events.Count() == 0)
        return Results.NotFound();

    var sensor = Sensor.Create(sensorId, events);
    return Results.Ok(sensor);
});

app.Run();

public record Sensor
{
    private Sensor(Guid id, Reading[]? readings)
    {
        this.Id = id;
        this.Readings = readings ?? Array.Empty<Reading>();

        this.Average = this.Readings.Select(r => r.Temperature).Average();
    }

    public Guid Id { get; }
    
    public Reading[] Readings { get; }

    public double Average { get; }

    public static Sensor Create(Guid id, IEnumerable<Event> events)
    {
        var readings = events.Where(evt => evt.Type == "ReadingReceived")
                             .Select(evt => JsonSerializer.Deserialize<Reading>(evt.Data.Span))
                             .ToArray();

        return new Sensor(id, readings);
    }
}

public record Reading(double Temperature, DateTimeOffset When);

public record ReadingReceived(double Temperature, DateTimeOffset When);