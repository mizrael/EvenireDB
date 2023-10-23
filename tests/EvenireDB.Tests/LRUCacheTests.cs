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

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(100)]
        public async Task GetOrAddAsync_should_be_atomic(int count)
        {
            var cache = new LRUCache<string, int>(1);
            var key = "lorem";

            var flags = new bool[count];
            var results = new int[count];
            var tasks = Enumerable.Range(0, count)
                .Select(async i =>
                {
                    results[i] = await cache.GetOrAddAsync(key, (_, _) => {
                            flags[i] = true;
                            return ValueTask.FromResult(i);
                        });
                }).ToArray();
            await Task.WhenAny(tasks);

            flags.Count(f => f).Should().Be(1);
            results.All(r => r == results[0]);
        }
    }
}