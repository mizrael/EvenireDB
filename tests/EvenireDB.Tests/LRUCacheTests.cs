namespace EvenireDB.Tests
{
    public class LRUCacheTests
    {
        [Fact]
        public async Task GetOrAddAsync_should_add_when_not_existing()
        {
            var cache = new LRUCache<string, string>(1);
            cache.Count.Should().Be(0);

            var value = await cache.GetOrAddAsync("key", (_, _) => new ValueTask<string>("value"));
            value.Should().Be("value");

            cache.Count.Should().Be(1);
        }

        [Fact]
        public async Task GetOrAddAsync_should_keep_capacity()
        {
            var cache = new LRUCache<string, string>(1);
            cache.Count.Should().Be(0);

            var value = await cache.GetOrAddAsync("key", (_, _) => new ValueTask<string>("value"));
            value.Should().Be("value");

            cache.Count.Should().Be(1);

            value = await cache.GetOrAddAsync("key2", (_, _) => new ValueTask<string>("value2"));
            value.Should().Be("value2");

            cache.Count.Should().Be(1);
        }
    }
}