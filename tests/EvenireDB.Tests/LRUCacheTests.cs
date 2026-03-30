namespace EvenireDB.Tests;

public class LRUCacheTests
{
    [Fact]
    public async Task GetOrAddAsync_should_add_when_not_existing()
    {
        var sut = new LRUCache<string, string>(1);
        Assert.Equal(0u, sut.Count);

        var value = await sut.GetOrAddAsync("key", (_, _) => new ValueTask<string>("value"));
        Assert.Equal("value", value);

        Assert.Equal(1u, sut.Count);
    }

    [Fact]
    public async Task GetOrAddAsync_should_keep_capacity()
    {
        var sut = new LRUCache<string, string>(1);
        Assert.Equal(0u, sut.Count);

        var value = await sut.GetOrAddAsync("key", (_, _) => new ValueTask<string>("value"));
        Assert.Equal("value", value);

        Assert.Equal(1u, sut.Count);

        value = await sut.GetOrAddAsync("key2", (_, _) => new ValueTask<string>("value2"));
        Assert.Equal("value2", value);

        Assert.Equal(1u, sut.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task GetOrAddAsync_should_be_atomic(int count)
    {
        var sut = new LRUCache<string, int>(1);
        var key = "lorem";

        var flags = new bool[count];
        var results = new int[count];
        var tasks = Enumerable.Range(0, count)
            .Select(async i =>
            {
                results[i] = await sut.GetOrAddAsync(key, (_, _) =>
                {
                    flags[i] = true;
                    return ValueTask.FromResult(i);
                });
            }).ToArray();
        await Task.WhenAny(tasks);

        Assert.Equal(1, flags.Count(f => f));
        Assert.True(results.All(r => r == results[0]));
    }

    [Fact]
    public async Task GetOrAdd_should_remove_old_items_when_capacity_reached()
    {
        var sut = new LRUCache<string, int>(1);

        await sut.GetOrAddAsync("key1", (_, _) => ValueTask.FromResult(1));
        Assert.Equal(1u, sut.Count);

        await sut.GetOrAddAsync("key2", (_, _) => ValueTask.FromResult(1));
        Assert.Equal(1u, sut.Count);

        Assert.False(sut.ContainsKey("key1"));
    }

    [Fact]
    public async Task GetOrAdd_should_put_item_on_front()
    {
        var sut = new LRUCache<string, int>(2);
        await sut.GetOrAddAsync("key1", (_, _) => ValueTask.FromResult(1));
        await sut.GetOrAddAsync("key2", (_, _) => ValueTask.FromResult(2));

        await sut.GetOrAddAsync("key1", null!);

        await sut.GetOrAddAsync("key3", (_, _) => ValueTask.FromResult(3));
        Assert.False(sut.ContainsKey("key2"));
    }

    [Fact]
    public async Task Upsert_should_update_existing_value()
    {
        var sut = new LRUCache<string, int>(10);
        await sut.GetOrAddAsync("key", (_, _) => ValueTask.FromResult(1));

        sut.AddOrUpdate("key", 2);
        var result = await sut.GetOrAddAsync("key", (_, _) => ValueTask.FromResult(1));
        Assert.Equal(1u, sut.Count);
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task Upsert_should_not_increase_size_when_key_exists()
    {
        var sut = new LRUCache<string, int>(10);
        await sut.GetOrAddAsync("key", (_, _) => ValueTask.FromResult(1));

        sut.AddOrUpdate("key", 2);
        Assert.Equal(1u, sut.Count);

        sut.AddOrUpdate("key", 2);
        Assert.Equal(1u, sut.Count);
    }

    [Fact]
    public void Upsert_should_add_when_item_does_not_exist()
    {
        var sut = new LRUCache<string, int>(10);

        sut.AddOrUpdate("key1", 71);
        Assert.Equal(1u, sut.Count);

        sut.AddOrUpdate("key2", 42);
        Assert.Equal(2u, sut.Count);
    }

    [Fact]
    public void Upsert_should_put_item_on_front()
    {
        var sut = new LRUCache<string, int>(2);

        sut.AddOrUpdate("key1", 1);
        sut.AddOrUpdate("key2", 2);

        sut.AddOrUpdate("key1", 42);

        sut.AddOrUpdate("key3", 3);

        Assert.False(sut.ContainsKey("key2"));
    }

    [Fact]
    public async Task DropOldest_should_not_exceed_count()
    {
        uint capacity = 10;
        var sut = new LRUCache<string, int>(capacity);

        for (int i = 0; i < capacity / 2; i++)
            await sut.GetOrAddAsync(i.ToString(), (_, _) => ValueTask.FromResult(i));

        Assert.Equal(capacity / 2, sut.Count);

        sut.DropOldest(sut.Count + 1);

        Assert.Equal(0u, sut.Count);
    }

    [Fact]
    public async Task DropOldest_should_remove_count_item()
    {
        uint capacity = 10;
        var sut = new LRUCache<string, int>(capacity);

        for (int i = 0; i < capacity; i++)
            await sut.GetOrAddAsync(i.ToString(), (_, _) => ValueTask.FromResult(i));

        sut.DropOldest(4);

        Assert.Equal(6u, sut.Count);
    }

    [Fact]
    public void Remove_should_do_nothing_when_key_not_present()
    {
        var sut = new LRUCache<string, int>(10);
        sut.Remove("lorem");
        Assert.Equal(0u, sut.Count);

        sut.AddOrUpdate("lorem", 1);
        Assert.Equal(1u, sut.Count);

        sut.Remove("ipsum");
        Assert.Equal(1u, sut.Count);
    }

    [Fact]
    public void Remove_should_empty_cache_when_only_entry()
    {
        var sut = new LRUCache<string, int>(10);

        sut.AddOrUpdate("lorem", 1);
        Assert.Equal(1u, sut.Count);

        sut.Remove("lorem");
        Assert.Equal(0u, sut.Count);
    }

    [Fact]
    public void Remove_should_remove_entry()
    {
        var sut = new LRUCache<string, int>(10);

        sut.AddOrUpdate("lorem", 1);
        sut.AddOrUpdate("ipsum", 1);
        sut.AddOrUpdate("dolor", 1);
        Assert.Equal(3u, sut.Count);

        sut.Remove("ipsum");
        Assert.Equal(2u, sut.Count);
        Assert.False(sut.ContainsKey("ipsum"));
    }
}
