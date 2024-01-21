using BenchmarkDotNet.Attributes;
using EvenireDB;
using EvenireDB.Extents;
using System.Buffers;

public class FileEventsRepositoryWriteBenckmarks
{
    private readonly static byte[] _data = Enumerable.Repeat((byte)42, 100).ToArray();

    private FileEventsRepositoryConfig _repoConfig;
    private IExtentInfoProvider _extentInfoProvider;
    private IEventDataValidator _factory;

    private Event[] BuildEvents(int count)
        => Enumerable.Range(0, count).Select(i => new Event(new EventId(i, 0), "lorem", _data)).ToArray();

    [GlobalSetup]
    public void Setup()
    {
        var basePath = AppContext.BaseDirectory;
        var dataPath = Path.Combine(basePath, "data");

        if(!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        _factory = new EventDataValidator(500_000);
        _extentInfoProvider = new ExtentInfoProvider(new ExtentInfoProviderConfig(dataPath));
        _repoConfig = new FileEventsRepositoryConfig();
    }

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Data))]    
    public async Task WriteAsync_Baseline(Event[] events)
    {
        var sut = new FileEventsRepository(_repoConfig, _extentInfoProvider);
        await sut.AppendAsync(Guid.NewGuid(), events);
    }

    public IEnumerable<Event[]> Data()
    {
        yield return BuildEvents(1_000);
        yield return BuildEvents(10_000);
        yield return BuildEvents(100_000);
    }
}
