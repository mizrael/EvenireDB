using EvenireDB.Client;
using EvenireDB.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = new Uri(builder.Configuration.GetConnectionString("evenire"));

builder.Services.AddHostedService<SensorsFakeProducer>()
                .Configure<Settings>(builder.Configuration.GetSection("Settings"))
                .AddEvenireDB(new EvenireConfig(connectionString, true));

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/sensors", ([FromServices] IOptions<Settings> config) =>
{
    return config.Value.SensorIds.Select(id => new
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
                             .Select(evt => JsonSerializer.Deserialize<Reading>(evt.Data))
                             .ToArray();

        return new Sensor(id, readings);
    }
}

public record Reading(double Temperature, DateTimeOffset When);

public record ReadingReceived(double Temperature, DateTimeOffset When);