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
        public async Task AppendAsync_should_write_events(int eventsCount, int expectedFileSize)
        {
            var events = _fixture.BuildEvents(eventsCount, new byte[] { 0x42 });

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            await sut.AppendAsync(streamId, events).ConfigureAwait(false);

            var eventsFilePath = Path.Combine(config.BasePath, streamId + "_data.dat");
            var bytes = File.ReadAllBytes(eventsFilePath);
            Assert.Equal(expectedFileSize, bytes.Length);
        }

        [Theory]
        [InlineData(10, 10, 100)]
        public async Task AppendAsync_should_append_events(int batchesCount, int eventsPerBatch, int expectedFileSize)
        {
            var batches = Enumerable.Range(0, batchesCount)
                .Select(b => _fixture.BuildEvents(eventsPerBatch, new byte[] { 0x42 }))
                .ToArray();

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            foreach(var events in batches)
                await sut.AppendAsync(streamId, events).ConfigureAwait(false);

            var eventsFilePath = Path.Combine(config.BasePath, streamId + "_data.dat");
            var bytes = File.ReadAllBytes(eventsFilePath);
            Assert.Equal(expectedFileSize, bytes.Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task ReadAsync_should_read_entire_stream(int eventsCount)
        {
            var expectedEvents = _fixture.BuildEvents(eventsCount);

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            await sut.AppendAsync(streamId, expectedEvents).ConfigureAwait(false);

            var events = await sut.ReadAsync(streamId).ToArrayAsync().ConfigureAwait(false);
            events.Should().NotBeNullOrEmpty()
                  .And.BeEquivalentTo(expectedEvents);
        }

        [Theory]
        [InlineData(10, 10)]
        public async Task ReadAsync_should_read_appended_events(int batchesCount, int eventsPerBatch)
        {
            var batches = Enumerable.Range(0, batchesCount)
                .Select(b => _fixture.BuildEvents(eventsPerBatch, new byte[] { 0x42 }))
                .ToArray();

            var streamId = Guid.NewGuid();
            var config = _fixture.CreateRepoConfig(streamId);
            var sut = new FileEventsRepository(config, new EventFactory(1000));
            foreach (var events in batches)
                await sut.AppendAsync(streamId, events).ConfigureAwait(false);

            var expectedEvents = batches.SelectMany(e => e).ToArray();

            var loadedEvents = await sut.ReadAsync(streamId).ToArrayAsync().ConfigureAwait(false);
            loadedEvents.Should().NotBeNullOrEmpty()
                         .And.BeEquivalentTo(expectedEvents);
        }
    }
}