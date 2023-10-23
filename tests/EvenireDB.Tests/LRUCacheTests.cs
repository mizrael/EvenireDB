namespace EvenireDB.Tests
{
    public class LRUCacheTests
    {
        [Fact]
        public async Task GetOrAddAsync_should_add_when_not_existing()
        {
            var sut = new LRUCache<string, string>(1);
            sut.Count.Should().Be(0);

            var value = await sut.GetOrAddAsync("key", (_, _) => new ValueTask<string>("value"));
            value.Should().Be("value");

            sut.Count.Should().Be(1);
        }

        [Fact]
        public async Task GetOrAddAsync_should_keep_capacity()
        {
            var sut = new LRUCache<string, string>(1);
            sut.Count.Should().Be(0);

            var value = await sut.GetOrAddAsync("key", (_, _) => new ValueTask<string>("value"));
            value.Should().Be("value");

            sut.Count.Should().Be(1);

            value = await sut.GetOrAddAsync("key2", (_, _) => new ValueTask<string>("value2"));
            value.Should().Be("value2");

            sut.Count.Should().Be(1);
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
                    results[i] = await sut.GetOrAddAsync(key, (_, _) => {
                            flags[i] = true;
                            return ValueTask.FromResult(i);
                        });
                }).ToArray();
            await Task.WhenAny(tasks);

            flags.Count(f => f).Should().Be(1);
            results.All(r => r == results[0]).Should().BeTrue();
        }

        [Fact]
        public async Task GetOrAdd_should_remove_old_items_when_capacity_reached()
        {
            var sut = new LRUCache<string, int>(1);

            await sut.GetOrAddAsync("key1", (_, _) => ValueTask.FromResult(1));
            sut.Count.Should().Be(1);

            await sut.GetOrAddAsync("key2", (_, _) => ValueTask.FromResult(1));
            sut.Count.Should().Be(1);

            sut.ContainsKey("key1").Should().BeFalse();
        }

        [Fact]
        public async Task GetOrAdd_should_put_item_on_front()
        {
            var sut = new LRUCache<string, int>(2);
            await sut.GetOrAddAsync("key1", (_, _) => ValueTask.FromResult(1));
            await sut.GetOrAddAsync("key2", (_, _) => ValueTask.FromResult(2));

            await sut.GetOrAddAsync("key1", null);

            await sut.GetOrAddAsync("key3", (_, _) => ValueTask.FromResult(3));
            sut.ContainsKey("key2").Should().BeFalse();
        }

        [Fact]
        public async Task Upsert_should_update_existing_value()
        {
            var sut = new LRUCache<string, int>(10);
            await sut.GetOrAddAsync("key", (_,_) => ValueTask.FromResult(1));
            
            sut.AddOrUpdate("key", 2);
            var result = await sut.GetOrAddAsync("key", (_, _) => ValueTask.FromResult(1));
            result.Should().Be(2);
        }

        [Fact]
        public async Task Upsert_should_not_increase_size_when_key_exists()
        {
            var sut = new LRUCache<string, int>(10);
            await sut.GetOrAddAsync("key", (_,_) => ValueTask.FromResult(1));
            
            sut.AddOrUpdate("key", 2);
            sut.Count.Should().Be(1);

            sut.AddOrUpdate("key", 2);
            sut.Count.Should().Be(1);
        }

        [Fact]
        public void Upsert_should_add_when_item_does_not_exist()
        {
            var sut = new LRUCache<string, int>(10);
            
            sut.AddOrUpdate("key1", 71);
            sut.Count.Should().Be(1);

            sut.AddOrUpdate("key2", 42);
            sut.Count.Should().Be(2);
        }

        [Fact]
        public void Upsert_should_put_item_on_front()
        {
            var sut = new LRUCache<string, int>(2);
                        
            sut.AddOrUpdate("key1", 1);
            sut.AddOrUpdate("key2", 2);

            sut.AddOrUpdate("key1", 42);

            sut.AddOrUpdate("key3", 3);
                        
            sut.ContainsKey("key2").Should().BeFalse();
        }

        [Fact]
        public async Task DropOldest_should_not_exceed_count()
        {
            uint capacity = 10;
            var sut = new LRUCache<string, int>(capacity);

            for(int i=0;i<capacity/2;i++)
                await sut.GetOrAddAsync(i.ToString(), (_, _) => ValueTask.FromResult(i));

            sut.Count.Should().Be(capacity / 2);

            sut.DropOldest(sut.Count + 1);
            
            sut.Count.Should().Be(0);
        }

        [Fact]
        public async Task DropOldest_should_remove_count_item()
        {
            uint capacity = 10;
            var sut = new LRUCache<string, int>(capacity);

            for (int i = 0; i < capacity; i++)
                await sut.GetOrAddAsync(i.ToString(), (_, _) => ValueTask.FromResult(i));

            sut.DropOldest(4);

            sut.Count.Should().Be(6);
        }
    }
}