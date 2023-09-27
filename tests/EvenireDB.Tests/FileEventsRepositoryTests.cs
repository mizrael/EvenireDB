namespace EvenireDB.Tests
{
    public class FileEventsRepositoryTests : IClassFixture<DataFixture>
    {
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
            var events = _fixture.BuildEvents(eventsCount, new byte[] { 0x42 });

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            await sut.WriteAsync(streamId, events).ConfigureAwait(false);

            var eventsFilePath = Path.Combine(config.BasePath, streamId + "_data.dat");
            var bytes = File.ReadAllBytes(eventsFilePath);
            Assert.Equal(expectedFileSize, bytes.Length);
        }

        [Theory]
        [InlineData(10, 10, 100)]
        public async Task WriteAsync_should_append_events(int batchesCount, int eventsPerBatch, int expectedFileSize)
        {
            var batches = Enumerable.Range(0, batchesCount)
                .Select(b => _fixture.BuildEvents(eventsPerBatch, new byte[] { 0x42 }))
                .ToArray();

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            foreach(var events in batches)
                await sut.WriteAsync(streamId, events).ConfigureAwait(false);

            var eventsFilePath = Path.Combine(config.BasePath, streamId + "_data.dat");
            var bytes = File.ReadAllBytes(eventsFilePath);
            Assert.Equal(expectedFileSize, bytes.Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task ReadAsync_should_read_events(int eventsCount)
        {
            var expectedEvents = _fixture.BuildEvents(eventsCount);

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            await sut.WriteAsync(streamId, expectedEvents).ConfigureAwait(false);

            var events = await sut.ReadAsync(streamId).ConfigureAwait(false);
            events.Should().NotBeNullOrEmpty()
                  .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_read_events_backwards()
        {
            var inputEvents = _fixture.BuildEvents(142);
            var expectedEvents = inputEvents.Reverse().Take(100).ToArray();

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            await sut.WriteAsync(streamId, inputEvents).ConfigureAwait(false);

            var events = await sut.ReadAsync(streamId, direction: Direction.Backward).ConfigureAwait(false);
            events.Should().NotBeNullOrEmpty()
                  .And.BeEquivalentTo(expectedEvents);
        }

        [Theory]
        [InlineData(10, 10, 100)]
        public async Task ReadAsync_should_read_subsequent_events(int batchesCount, int eventsPerBatch, int expectedFileSize)
        {
            var batches = Enumerable.Range(0, batchesCount)
                .Select(b => _fixture.BuildEvents(eventsPerBatch, new byte[] { 0x42 }))
                .ToArray();

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            foreach (var events in batches)
                await sut.WriteAsync(streamId, events).ConfigureAwait(false);

            var expectedEvents = batches.SelectMany(e => e).ToArray();

            var loadedEvents = await sut.ReadAsync(streamId).ConfigureAwait(false);
            loadedEvents.Should().NotBeNullOrEmpty()
                         .And.BeEquivalentTo(expectedEvents);
        }

        [Fact]
        public async Task ReadAsync_should_read_events_when_offset_provided()
        {
            var expectedEvents = _fixture.BuildEvents(142);
            var expectedEvent = expectedEvents.Skip(42).First();

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));

            await sut.WriteAsync(streamId, expectedEvents).ConfigureAwait(false);

            var events = await sut.ReadAsync(streamId, skip: 42).ConfigureAwait(false);
            events.Should().NotBeNullOrEmpty()
                  .And.HaveCount(100);
            events.ElementAt(0).Id.Should().Be(expectedEvent.Id);
        }

        [Fact]
        public async Task ReadAsync_should_read_only_page_size_events()
        {
            var expectedEvents = _fixture.BuildEvents(142);

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            await sut.WriteAsync(streamId, expectedEvents).ConfigureAwait(false);

            var events = await sut.ReadAsync(streamId, skip: 0).ConfigureAwait(false);
            events.Should().NotBeNullOrEmpty()
                  .And.HaveCount(100);
        }

        [Fact]
        public async Task ReadAsync_should_return_empty_collection_when_skip_to_high()
        {
            var expectedEvents = _fixture.BuildEvents(10);

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            await sut.WriteAsync(streamId, expectedEvents).ConfigureAwait(false);

            var events = await sut.ReadAsync(streamId, skip: 11).ConfigureAwait(false);
            events.Should().NotBeNull()
                  .And.BeEmpty();
        }
    }
}