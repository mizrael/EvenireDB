// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;

public class FileEventsRepositoryBenckmarks
{
    private readonly static byte[] _data = Enumerable.Repeat((byte)42, 1000).ToArray();

    private Event[] BuildEvents(int count)
        => Enumerable.Range(0, count).Select(i => new Event("lorem", _data, i)).ToArray();

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Data))]    
    public async Task WriteAsync_Baseline(Event[] events)
    {
        using var sut = new FileEventsRepository("./data");
        await sut.WriteAsync(Guid.NewGuid(), events);
    }

    public IEnumerable<Event[]> Data()
    {
        yield return BuildEvents(100);
  /*      yield return BuildEvents(1_000);
        yield return BuildEvents(10_000);
        yield return BuildEvents(100_000);
        yield return BuildEvents(1_000_000);
        yield return BuildEvents(10_000_000);*/
    }
}