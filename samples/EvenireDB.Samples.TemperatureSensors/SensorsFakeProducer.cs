using EvenireDB.Client;
using EvenireDB.Common;
using Microsoft.Extensions.Logging;

namespace EvenireDB.Samples.TemperatureSensors;

public class SensorsFakeProducer : BackgroundService
{
    private static readonly TimeSpan _delay = TimeSpan.FromSeconds(10);
    private readonly IEventsClient _eventsClient;
    private readonly Settings _sensorConfig;
    private readonly ILogger<SensorsFakeProducer> _logger;

    public SensorsFakeProducer(IEventsClient eventsClient, Settings sensorConfig, ILogger<SensorsFakeProducer> logger)
    {
        _eventsClient = eventsClient;
        _sensorConfig = sensorConfig;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait briefly for the server to be ready
        await Task.Delay(2000, stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var sensorId in _sensorConfig.SensorIds)
            {
                try
                {
                    var reading = new ReadingReceived(
                        Math.Round(Random.Shared.NextDouble() * 100, 1),
                        DateTimeOffset.UtcNow);

                    var streamId = new StreamId(sensorId, nameof(Sensor));

                    await _eventsClient.AppendAsync(streamId, new[]
                    {
                        EvenireDB.Client.EventData.Create(reading),
                    }, stoppingToken).ConfigureAwait(false);

                    _logger.LogDebug("Sensor {SensorId}: {Temperature}°C", sensorId, reading.Temperature);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Failed to send reading for sensor {SensorId}", sensorId);
                }
            }

            await Task.Delay(_delay, stoppingToken).ConfigureAwait(false);
        }
    }
}
