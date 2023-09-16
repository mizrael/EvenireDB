namespace EvenireDB.Tests
{
    public class FileEventsRepositoryTests : IClassFixture<DataFixture>
    {
        private readonly static byte[] _data = Enumerable.Repeat((byte)42, 1000).ToArray();
        private readonly DataFixture _fixture;

        public FileEventsRepositoryTests(DataFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData(1, 1025)]
        [InlineData(10, 10250)]
        public async Task WriteAsync_should_write_events(int eventsCount, int expectedFileSize)
        {
            var events = BuildEvents(eventsCount);

            var aggregateId = Guid.NewGuid();
            var dataFolder = _fixture.CreateAggregateDataFolder(aggregateId);
            using (var sut = new FileEventsRepository(dataFolder))
            {
                await sut.WriteAsync(aggregateId, events).ConfigureAwait(false);
            }

            var eventsFilePath = Path.Combine(dataFolder, aggregateId + ".dat");
            var bytes = File.ReadAllBytes(eventsFilePath);
            Assert.Equal(expectedFileSize, bytes.Length);            
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task ReadAsync_should_read_events(int eventsCount)
        {
            var expectedEvents = BuildEvents(eventsCount);

            var aggregateId = Guid.NewGuid();
            var dataFolder = _fixture.CreateAggregateDataFolder(aggregateId);
            using (var sut = new FileEventsRepository(dataFolder))
            {
                await sut.WriteAsync(aggregateId, expectedEvents).ConfigureAwait(false);
                var events = await sut.ReadAsync(aggregateId).ConfigureAwait(false);
                events.Should().NotBeNullOrEmpty()
                      .And.BeEquivalentTo(expectedEvents);                
            }
        }

        private Event[] BuildEvents(int count)
            => Enumerable.Range(0, count).Select(i => new Event("lorem", _data, i)).ToArray();
    }
}