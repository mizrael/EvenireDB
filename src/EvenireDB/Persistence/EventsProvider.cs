﻿using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace EvenireDB.Persistence;

internal class EventsProvider : IEventsProvider
{
    private readonly IStreamInfoProvider _extentInfoProvider;
    private readonly IHeadersRepository _headersRepo;
    private readonly IDataRepository _dataRepo;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _streamLocks = new();

    public EventsProvider(IHeadersRepository headersRepo, IDataRepository dataRepo, IStreamInfoProvider extentInfoProvider)
    {
        _headersRepo = headersRepo;
        _dataRepo = dataRepo;
        _extentInfoProvider = extentInfoProvider;
    }

    public async IAsyncEnumerable<Event> ReadAsync(
        Guid streamId,
        int? skip = null,
        int? take = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var extentInfo = _extentInfoProvider.GetExtentInfo(streamId);
        if (!File.Exists(extentInfo.DataPath) || !File.Exists(extentInfo.HeadersPath))
            yield break;

        var headersCursor = _headersRepo.ReadAsync(extentInfo, skip, take, cancellationToken);
        await foreach (var @event in _dataRepo.ReadAsync(extentInfo, headersCursor, cancellationToken))
        {
            yield return @event;
        }
    }

    public async ValueTask AppendAsync(
        Guid streamId,
        IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
        var extentInfo = _extentInfoProvider.GetExtentInfo(streamId);

        var semaphore = _streamLocks.GetOrAdd(streamId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var inHeaders = _dataRepo.AppendAsync(extentInfo, events, cancellationToken);
            await _headersRepo.AppendAsync(extentInfo, inHeaders, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }   
    }
}