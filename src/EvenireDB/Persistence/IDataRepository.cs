using EvenireDB.Extents;

namespace EvenireDB.Persistence; 

internal interface IDataRepository
{
    IAsyncEnumerable<RawHeader> AppendAsync(ExtentInfo extentInfo, IEnumerable<Event> events, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Event> ReadAsync(ExtentInfo extentInfo, IAsyncEnumerable<RawHeader> headers, CancellationToken cancellationToken = default);
}