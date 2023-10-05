using BenchmarkDotNet.Attributes;
using System.Text;

[MemoryDiagnoser]
public class GetBytesBenchmark
{
    private string _input;

    [Params(10, 100, 1000, 10000)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _input = new string( Enumerable.Repeat('c', this.Length).ToArray() );
    }

    [Benchmark(Baseline = true)]
    public byte[] GetBytesAndCopy()
    {
        var src = Encoding.UTF8.GetBytes(_input);
        var dest = new byte[_input.Length];
        Array.Copy(src, dest, src.Length);
        return dest;
    }

    [Benchmark()]
    public byte[] GetBytesToDest()
    {
        var dest = new byte[_input.Length];
        Encoding.UTF8.GetBytes(_input, dest);
        return dest;
    }
}
