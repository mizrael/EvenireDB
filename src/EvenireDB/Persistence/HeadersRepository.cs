using EvenireDB.Extents;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EvenireDB.Persistence;

internal class HeadersRepository : IHeadersRepository
{
    private readonly int _headerSize = Marshal.SizeOf<RawHeader>();

    public async ValueTask AppendAsync(ExtentInfo extentInfo, IAsyncEnumerable<RawHeader> headers, CancellationToken cancellationToken = default)
    {
        var headerSize = Marshal.SizeOf<RawHeader>();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(headerSize);

        var streamBufferSize = headerSize * 100;

        try
        {
            using var stream = new FileStream(extentInfo.HeadersPath, FileMode.Append, FileAccess.Write,
                                             FileShare.Read, bufferSize: streamBufferSize, useAsync: true);

            await foreach (var header in headers)
            {
                var tmpHeader = header;
                MemoryMarshal.Write(buffer.AsSpan(0, headerSize), in tmpHeader);
                await stream.WriteAsync(buffer, 0, headerSize, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async IAsyncEnumerable<RawHeader> ReadAsync(
        ExtentInfo extentInfo,
        int? skip = null,
        int? take = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var streamBufferSize = _headerSize * take.GetValueOrDefault(100);

        using var stream = new FileStream(extentInfo.HeadersPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: streamBufferSize, useAsync: true);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(_headerSize);

        try
        {
            if (skip.HasValue)
                stream.Seek(skip.Value * _headerSize, SeekOrigin.Begin);

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, _headerSize, cancellationToken);
                if (bytesRead == 0)
                    yield break;

                if (take.HasValue)
                {
                    take--;
                    if (take.Value < 0)
                        yield break;
                }

                var header = MemoryMarshal.Read<RawHeader>(buffer.AsSpan(0, _headerSize));
                yield return header;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
