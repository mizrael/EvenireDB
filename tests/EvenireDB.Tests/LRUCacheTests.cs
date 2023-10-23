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
            results.All(r => r == results[0]).Should().BeTrue();
        }

        [Fact]
        public void Update_should_throw_when_key_not_existing()
        {
            var cache = new LRUCache<string, int>(1);
            Assert.Throws<KeyNotFoundException>(() => cache.Update("key", 1));
        }

        [Fact]
        public async Task Update_should_update_value()
        {
            var cache = new LRUCache<string, int>(1);
            await cache.GetOrAddAsync("key", (_,_) => ValueTask.FromResult(1));
            
            cache.Update("key", 2);
            var result = await cache.GetOrAddAsync("key", (_, _) => ValueTask.FromResult(1));
            result.Should().Be(2);
        }

        [Fact]
        public async Task Update_should_not_increase_size()
        {
            var cache = new LRUCache<string, int>(1);
            await cache.GetOrAddAsync("key", (_,_) => ValueTask.FromResult(1));
            
            cache.Update("key", 2);
            cache.Count.Should().Be(1);

            cache.Update("key", 2);
            cache.Count.Should().Be(1);
        }
    }
}