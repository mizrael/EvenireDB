using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EvenireDB.Persistence;

internal record HeadersRepositorySettings(
    int AppendBatchCapacity = 512,
    int MaxPageSize = 100);

internal class HeadersRepository : IHeadersRepository
{
    private static readonly int HeaderSize = Unsafe.SizeOf<RawHeader>();

    private readonly HeadersRepositorySettings _settings;

    public HeadersRepository(HeadersRepositorySettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async ValueTask AppendAsync(
        ExtentInfo extentInfo,
        IAsyncEnumerable<RawHeader> headers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(extentInfo);

        var headerBatch = ArrayPool<RawHeader>.Shared.Rent(_settings.AppendBatchCapacity);
        int batchCount = 0;

        using var stream = new FileStream(
            extentInfo.HeadersPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: HeaderSize * _settings.AppendBatchCapacity,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        try
        {
            await foreach (var header in headers.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                headerBatch[batchCount++] = header;

                if (batchCount == _settings.AppendBatchCapacity)
                {
                    await WriteBatchAsync(stream, headerBatch, batchCount, cancellationToken).ConfigureAwait(false);
                    batchCount = 0;
                }
            }

            if (batchCount > 0)
            {
                await WriteBatchAsync(stream, headerBatch, batchCount, cancellationToken).ConfigureAwait(false);
            }

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<RawHeader>.Shared.Return(headerBatch, clearArray: false);
        }
    }

    public async IAsyncEnumerable<RawHeader> ReadAsync(
        ExtentInfo extentInfo,
        int? skip = null,
        int? take = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(extentInfo);

        // Calculate how many headers we actually aim to read per batch.
        int batchTarget = take.HasValue
            ? Math.Min(_settings.MaxPageSize, Math.Max(1, take.Value))
            : _settings.MaxPageSize;

        // Rent byte buffer sized for batch of headers.
        int byteBatchSize = HeaderSize * batchTarget;
        byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(byteBatchSize);

        using var stream = new FileStream(
            extentInfo.HeadersPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: byteBatchSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        try
        {
            if (skip.HasValue && skip.Value > 0)
            {
                long offset = (long)skip.Value * HeaderSize;
                if (offset > stream.Length)
                    yield break; // skip beyond end
                stream.Seek(offset, SeekOrigin.Begin);
            }

            var remaining = take ?? int.MaxValue;

            while (!cancellationToken.IsCancellationRequested && remaining > 0)
            {
                // Adjust last batch if we have a bounded remaining.
                int headersToReadThisBatch = Math.Min(batchTarget, remaining);
                int bytesToRead = headersToReadThisBatch * HeaderSize;

                int totalRead = 0;
                while (totalRead < bytesToRead)
                {
                    int read = await stream.ReadAsync(byteBuffer, totalRead, bytesToRead - totalRead, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        remaining = 0;
                        break;
                    }

                    totalRead += read;
                }

                var bytes = byteBuffer.AsSpan(0, totalRead);
                var actualHeadersRead = bytes.Length / HeaderSize;
                var headersArray = new RawHeader[actualHeadersRead];
                MemoryMarshal.Cast<byte, RawHeader>(bytes).CopyTo(headersArray);

                for (int i = 0; i < headersArray.Length; i++)
                    yield return headersArray[i];

                remaining -= headersArray.Length;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer, clearArray: false);
        }
    }

    private static async ValueTask WriteBatchAsync(
        FileStream stream,
        RawHeader[] headers,
        int count,
        CancellationToken ct)
    {
        int byteLen = count * HeaderSize;
        byte[] rented = ArrayPool<byte>.Shared.Rent(byteLen);
        try
        {
            MemoryMarshal.AsBytes(headers.AsSpan(0, count)).CopyTo(rented.AsSpan(0, byteLen));
            await stream.WriteAsync(rented.AsMemory(0, byteLen), ct).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: false);
        }
    }
}