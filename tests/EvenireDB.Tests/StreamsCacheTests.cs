using EvenireDB.Common;
using EvenireDB.Persistence;
using EvenireDB.Utils;

namespace EvenireDB.Tests;

public class StreamsCacheTests
{
    private static readonly byte[] _defaultData = new byte[] { 0x42 };

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
            yield return item;

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<T> EmptyAsyncEnumerable<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    [Fact]
    public async Task GetEventsAsync_should_load_from_repository_on_cache_miss()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var persistedEvents = Enumerable.Range(0, 5)
            .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
            .ToList();

        var repo = Substitute.For<IEventsProvider>();
        repo.ReadAsync(streamId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(persistedEvents));

        var cache = new LRUCache<StreamId, CachedEvents>(100);
        var logger = Substitute.For<ILogger<StreamsCache>>();
        var sut = new StreamsCache(logger, cache, repo);

        var result = await sut.GetEventsAsync(streamId);

        Assert.NotNull(result);
        Assert.Equal(persistedEvents.Count, result.Events.Count);
        for (int i = 0; i < persistedEvents.Count; i++)
            Assert.Equal(persistedEvents[i].Id, result.Events[i].Id);
    }

    [Fact]
    public async Task GetEventsAsync_should_return_cached_data_on_second_call()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var persistedEvents = Enumerable.Range(0, 3)
            .Select(i => new Event(new EventId(i, 0), "lorem", _defaultData))
            .ToList();

        var repo = Substitute.For<IEventsProvider>();
        repo.ReadAsync(streamId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(persistedEvents));

        var cache = new LRUCache<StreamId, CachedEvents>(100);
        var logger = Substitute.For<ILogger<StreamsCache>>();
        var sut = new StreamsCache(logger, cache, repo);

        var first = await sut.GetEventsAsync(streamId);
        var second = await sut.GetEventsAsync(streamId);

        Assert.Same(first, second);
        repo.Received(1).ReadAsync(streamId, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEventsAsync_should_return_empty_for_new_stream()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var repo = Substitute.For<IEventsProvider>();
        repo.ReadAsync(streamId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable<Event>());

        var cache = new LRUCache<StreamId, CachedEvents>(100);
        var logger = Substitute.For<ILogger<StreamsCache>>();
        var sut = new StreamsCache(logger, cache, repo);

        var result = await sut.GetEventsAsync(streamId);

        Assert.NotNull(result);
        Assert.Empty(result.Events);
    }

    [Fact]
    public async Task Contains_should_return_true_after_stream_is_loaded()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var repo = Substitute.For<IEventsProvider>();
        repo.ReadAsync(streamId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable<Event>());

        var cache = new LRUCache<StreamId, CachedEvents>(100);
        var logger = Substitute.For<ILogger<StreamsCache>>();
        var sut = new StreamsCache(logger, cache, repo);

        Assert.False(sut.Contains(streamId));
        await sut.GetEventsAsync(streamId);
        Assert.True(sut.Contains(streamId));
    }

    [Fact]
    public async Task Remove_should_evict_cached_stream()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var repo = Substitute.For<IEventsProvider>();
        repo.ReadAsync(streamId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable<Event>());

        var cache = new LRUCache<StreamId, CachedEvents>(100);
        var logger = Substitute.For<ILogger<StreamsCache>>();
        var sut = new StreamsCache(logger, cache, repo);

        await sut.GetEventsAsync(streamId);
        Assert.True(sut.Contains(streamId));

        sut.Remove(streamId);
        Assert.False(sut.Contains(streamId));
    }
}
