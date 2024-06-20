using EvenireDB.Client;
using System.Text.Json;

namespace EvenireDB.Samples.TemperatureSensors;

public record Sensor
{
    private Sensor(Guid id, Reading[]? readings)
    {
        this.Id = id;
        this.Readings = readings ?? [];

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
