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
        var encoding = Encoding.UTF8;

        var src = encoding.GetBytes(_input);
        var dest = new byte[src.Length];
        Array.Copy(src, dest, src.Length);
        return dest;
    }

    [Benchmark()]
    public byte[] GetBytesToDest()
    {
        var encoding = Encoding.UTF8;
        int maxChars = encoding.GetMaxByteCount(_input.Length);
        var dest = new byte[maxChars];
        encoding.GetBytes(_input, dest);
        return dest;
    }

    [Benchmark()]
    public Span<byte> GetBytesToDestAsSpan()
    {
        var encoding = Encoding.UTF8;
        int maxChars = encoding.GetMaxByteCount(_input.Length);
        
        var srcSpan = _input.AsSpan();
        Span<byte> dest = new byte[maxChars];

        var written = encoding.GetBytes(srcSpan, dest);
        return dest.Slice(0, written);
    }
}
