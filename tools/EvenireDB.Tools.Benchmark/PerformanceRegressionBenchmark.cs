using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[JsonExporterAttribute.Brief]
public class PerformanceRegressionBenchmark
{
    private EvenireDbEngine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new EvenireDbEngine();
    }

    [Benchmark(Baseline = true)]
    public async Task WriteEvent()
    {
        await _engine.WriteEventAsync(new TestEvent { Id = Guid.NewGuid() });
    }

    [Benchmark]
    public async Task ReadEvents()
    {
        await foreach (var evt in _engine.ReadEventsAsync(0, 100))
        {
            // Process event
        }
    }
}