using System.Text.Json;

namespace EvenireDB.Samples.TemperatureSensors;

public record Sensor
{
    private Sensor(Guid id, Reading[] readings)
    {
        Id = id;
        Readings = readings;
        Average = readings.Length > 0 ? readings.Average(r => r.Temperature) : 0;
    }

    public Guid Id { get; }

    public Reading[] Readings { get; }

    public double Average { get; }

    public static Sensor Create(Guid id, IEnumerable<EvenireDB.Client.Event> events)
    {
        var readings = events.Where(evt => evt.Type == nameof(ReadingReceived))
                             .Select(evt => JsonSerializer.Deserialize<Reading>(evt.Data.Span))
                             .Where(r => r is not null)
                             .Cast<Reading>()
                             .ToArray();

        return new Sensor(id, readings);
    }
}
