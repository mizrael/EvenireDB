using EvenireDB.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = new Uri(builder.Configuration.GetConnectionString("evenire"));

builder.Services.AddHostedService<SensorsFakeProducer>()
                .Configure<Settings>(builder.Configuration.GetSection("Settings"))
                .AddEvenireDB(new EvenireConfig(connectionString));

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
    var events = await client.ReadAsync(sensorId, StreamPosition.End, Direction.Backward).ConfigureAwait(false);

    if (events.Count() == 0)
        return Results.NotFound();

    var sensor = Sensor.Rehydrate(sensorId, events);
    return Results.Ok(sensor);
});

app.Run();

public record Sensor
{
    public Guid Id { get; init; }
    public Reading[]? Readings { get; init; }

    public static Sensor Rehydrate(Guid id, IEnumerable<Event> events)
    {
        var readings = events.Where(evt => evt.Type == "ReadingReceived")
                             .Select(evt => JsonSerializer.Deserialize<Reading>(evt.Data))
                             .ToArray();
        return new Sensor()
        {
            Id = id,
            Readings = readings
        };
    }
}

public record Reading(double Temperature, DateTimeOffset When);

public record ReadingReceived(double Temperature, DateTimeOffset When);