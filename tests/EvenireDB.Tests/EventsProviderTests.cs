using EvenireDB.Common;
using EvenireDB.Persistence;

namespace EvenireDB.Tests;

public class EventsProviderTests : IClassFixture<DataFixture>
{
    private readonly DataFixture _fixture;

    public EventsProviderTests(DataFixture fixture)
    {
        _fixture = fixture;
    }

    private EventsProvider CreateSut(out IExtentsProvider extentInfoProvider)
    {
        var config = _fixture.CreateExtentsConfig();
        extentInfoProvider = new ExtentsProvider(config);
        var dataRepo = new DataRepository();
        var headersRepo = new HeadersRepository();
        return new EventsProvider(headersRepo, dataRepo, extentInfoProvider);
    }

    [Theory]
    [InlineData(1, 51)]
    [InlineData(10, 510)]
    public async Task AppendAsync_should_write_events(int eventsCount, int expectedFileSize)
    {
        var events = _fixture.BuildEvents(eventsCount, new byte[] { 0x42 });

        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var sut = CreateSut(out var extentInfoProvider);

        await sut.AppendAsync(streamId, events);

        var extentInfo = extentInfoProvider.GetExtentInfo(streamId);
        var bytes = File.ReadAllBytes(extentInfo!.DataPath);
        Assert.Equal(expectedFileSize, bytes.Length);
    }

    [Theory]
    [InlineData(10, 10, 5100)]
    public async Task AppendAsync_should_append_events(int batchesCount, int eventsPerBatch, int expectedFileSize)
    {
        var batches = Enumerable.Range(0, batchesCount)
            .Select(b => _fixture.BuildEvents(eventsPerBatch, new byte[] { 0x42 }))
            .ToArray();

        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var sut = CreateSut(out var extentInfoProvider);

        foreach (var events in batches)
            await sut.AppendAsync(streamId, events);

        var extentInfo = extentInfoProvider.GetExtentInfo(streamId);
        var bytes = File.ReadAllBytes(extentInfo!.DataPath);
        Assert.Equal(expectedFileSize, bytes.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public async Task ReadAsync_should_read_entire_stream(int eventsCount)
    {
        var expectedEvents = _fixture.BuildEvents(eventsCount);

        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var sut = CreateSut(out var _);

        await sut.AppendAsync(streamId, expectedEvents);

        var events = await sut.ReadAsync(streamId).ToArrayAsync();
        Assert.NotNull(events);
        Assert.Equal(eventsCount, events.Length);

        for (int i = 0; i != eventsCount; i++)
        {
            Assert.Equal(expectedEvents[i].Id, events[i].Id);
            Assert.Equal(expectedEvents[i].Type, events[i].Type);
            Assert.Equal(expectedEvents[i].Data.ToArray(), events[i].Data.ToArray());
        }
    }

    [Theory]
    [InlineData(42, 71)]
    public async Task ReadAsync_should_read_events_appended_in_batches(int batchesCount, int eventsPerBatch)
    {
        var batches = Enumerable.Range(0, batchesCount)
            .Select(b => _fixture.BuildEvents(eventsPerBatch, new byte[] { 0x42 }))
            .ToArray();

        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var sut = CreateSut(out var _);

        foreach (var events in batches)
            await sut.AppendAsync(streamId, events);

        var expectedEvents = batches.SelectMany(e => e).ToArray();

        var loadedEvents = await sut.ReadAsync(streamId).ToListAsync();

        Assert.NotNull(loadedEvents);
        Assert.Equal(batchesCount * eventsPerBatch, loadedEvents.Count);
    }

    [Fact]
    public async Task ReadAsync_should_read_paged_events()
    {
        var expectedEvents = _fixture.BuildEvents(42);

        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var sut = CreateSut(out var _);

        await sut.AppendAsync(streamId, expectedEvents);

        var events = await sut.ReadAsync(streamId, 4, 10).ToArrayAsync();
        Assert.NotNull(events);
        Assert.Equal(10, events.Length);

        for (int i = 0; i != 10; i++)
        {
            Assert.Equal(expectedEvents[4 + i].Id, events[i].Id);
        }
    }

    [Fact]
    public async Task ReadAsync_should_return_no_data_when_paging_invalid()
    {
        var expectedEvents = _fixture.BuildEvents(42);

        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var sut = CreateSut(out var _);

        await sut.AppendAsync(streamId, expectedEvents);

        var events = await sut.ReadAsync(streamId, 100, 10).ToArrayAsync();
        Assert.NotNull(events);
        Assert.Empty(events);
    }
}