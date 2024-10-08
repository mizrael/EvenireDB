﻿using EvenireDB.Client;
using EvenireDB.Common;

namespace EvenireDB.Samples.TemperatureSensors;

public class SensorsFakeProducer : BackgroundService
{
    private readonly static TimeSpan _delay = TimeSpan.FromSeconds(10);
    private readonly IEventsClient _eventsClient;
    private readonly Settings _sensorConfig;

    public SensorsFakeProducer(IEventsClient eventsClient, Settings sensorConfig)
    {
        _eventsClient = eventsClient;
        _sensorConfig = sensorConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            foreach (var sensorId in _sensorConfig.SensorIds)
            {
                var reading = new ReadingReceived(Random.Shared.NextDouble() * 100, DateTimeOffset.UtcNow);

                var streamId = new StreamId(sensorId, nameof(Sensor));

                await _eventsClient.AppendAsync(streamId, new[]
                {
                    EventData.Create(reading),
                }, stoppingToken);
            }
            await Task.Delay(_delay);
        }
    }
}
