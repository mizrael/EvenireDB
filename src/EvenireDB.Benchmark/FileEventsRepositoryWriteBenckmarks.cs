using BenchmarkDotNet.Attributes;
using EvenireDB;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Buffers;
using System.Runtime.CompilerServices;

public class FileEventsRepositoryWriteBenckmarks
{
    private readonly static byte[] _data = Enumerable.Repeat((byte)42, 100).ToArray();

    private FileEventsRepositoryConfig _repoConfig;
    private IEventFactory _factory;

    private IEvent[] BuildEvents(int count)
        => Enumerable.Range(0, count).Select(i => _factory.Create(new EventId(42, 71), "lorem", _data)).ToArray();

    [GlobalSetup]
    public void Setup()
    {
        var basePath = AppContext.BaseDirectory;
        var dataPath = Path.Combine(basePath, "data");

        if(!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        _factory = new EventFactory(500_000);
        _repoConfig = new FileEventsRepositoryConfig(dataPath);
    }

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Data))]    
    public async Task WriteAsync_Baseline(IEvent[] events)
    {
        var sut = new FileEventsRepository(_repoConfig, _factory);
        await sut.AppendAsync(Guid.NewGuid(), events);
    }

    public IEnumerable<IEvent[]> Data()
    {
        yield return BuildEvents(1_000);
        yield return BuildEvents(10_000);
        yield return BuildEvents(100_000);
    }
}
