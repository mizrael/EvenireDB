﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using EvenireDB;
using EvenireDB.Common;
using EvenireDB.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Channels;

public class EventsProviderBenckmarks
{
    private readonly static byte[] _data = Enumerable.Repeat((byte)42, 100).ToArray();
    private readonly static Guid _streamId = Guid.NewGuid();
    private readonly Consumer _consumer = new Consumer();

    private EventsProvider _sut;

    [Params(10, 100, 1000)]
    public uint EventsCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var basePath = AppContext.BaseDirectory;
        var dataPath = Path.Combine(basePath, "data");

        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        var factory = new EventFactory(500_000);
        var repoConfig = new FileEventsRepositoryConfig(dataPath);
        var repo = new FileEventsRepository(repoConfig, factory);

        var cache = new LRUCache<Guid, CachedEvents>(this.EventsCount);
        var logger = new NullLogger<EventsProvider>();

        var channel = Channel.CreateUnbounded<IncomingEventsGroup>();

        _sut = new EventsProvider(EventsProviderConfig.Default, repo, cache, channel.Writer, logger);

        var events = Enumerable.Range(0, (int)this.EventsCount).Select(i => factory.Create(new EventId(42, 71), "lorem", _data)).ToArray();
        Task.WaitAll(_sut.AppendAsync(_streamId, events).AsTask());
    }

    [Benchmark(Baseline = true)]    
    public async Task ReadAsync_Baseline()
    {
        int count = 0;
        var events = await _sut.ReadAsync(_streamId, StreamPosition.Start).ToListAsync().ConfigureAwait(false);
        foreach (var @evt in events)
            count++;
    }
}