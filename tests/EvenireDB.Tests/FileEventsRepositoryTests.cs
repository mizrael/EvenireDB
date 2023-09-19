namespace EvenireDB.Tests
{
    public class FileEventsRepositoryTests : IClassFixture<DataFixture>
    {
        private readonly static byte[] _defaultEventData = new byte[] { 0x42 };
        private readonly DataFixture _fixture;

        public FileEventsRepositoryTests(DataFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        public async Task WriteAsync_should_write_events(int eventsCount, int expectedFileSize)
        {
            var events = BuildEvents(eventsCount);

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            using (var sut = new FileEventsRepository(config))
            {
                await sut.WriteAsync(streamId, events).ConfigureAwait(false);
            }

            var eventsFilePath = Path.Combine(config.BasePath, streamId + "_data.dat");
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
            var dataFolder = _fixture.CreateRepoConfig(aggregateId);
            using (var sut = new FileEventsRepository(dataFolder))
            {
                await sut.WriteAsync(aggregateId, expectedEvents).ConfigureAwait(false);

                var events = await sut.ReadAsync(aggregateId).ConfigureAwait(false);
                events.Should().NotBeNullOrEmpty()
                      .And.BeEquivalentTo(expectedEvents);
            }
        }

        [Fact]
        public async Task ReadAsync_should_read_events_when_offset_provided()
        {
            var expectedEvents = BuildEvents(142);
            var expectedEvent = expectedEvents.Skip(42).First();

            var aggregateId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(aggregateId);
            using (var sut = new FileEventsRepository(config))
            {
                await sut.WriteAsync(aggregateId, expectedEvents).ConfigureAwait(false);

                var events = await sut.ReadAsync(aggregateId, skip: 42).ConfigureAwait(false);
                events.Should().NotBeNullOrEmpty()
                      .And.HaveCount(100);
                events.ElementAt(0).Id.Should().Be(expectedEvent.Id);
            }
        }

        [Fact]
        public async Task ReadAsync_should_read_only_page_size_events()
        {
            var expectedEvents = BuildEvents(142);

            var aggregateId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(aggregateId);
            using (var sut = new FileEventsRepository(config))
            {
                await sut.WriteAsync(aggregateId, expectedEvents).ConfigureAwait(false);

                var events = await sut.ReadAsync(aggregateId, skip: 0).ConfigureAwait(false);
                events.Should().NotBeNullOrEmpty()
                      .And.HaveCount(100);                
            }
        }

        private Event[] BuildEvents(int count)
            => Enumerable.Range(0, count).Select(i => new Event(Guid.NewGuid(), "lorem", _defaultEventData)).ToArray();
    }
}