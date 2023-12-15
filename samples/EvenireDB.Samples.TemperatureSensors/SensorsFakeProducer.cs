using EvenireDB.Client;

public class SensorsFakeProducer : BackgroundService
{
    private readonly static TimeSpan _delay = TimeSpan.FromSeconds(30);
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
                await _eventsClient.AppendAsync(sensorId, new[]
                {
                    EventData.Create(reading),
                }, stoppingToken);
            }
            await Task.Delay(_delay);
        }
    }
}
