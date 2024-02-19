using EvenireDB.Extents;

namespace EvenireDB.Persistence; 

internal interface IHeadersRepository
{
    ValueTask AppendAsync(ExtentInfo extentInfo, IAsyncEnumerable<RawHeader> headers, CancellationToken cancellationToken = default);
    IAsyncEnumerable<RawHeader> ReadAsync(ExtentInfo extentInfo, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
}