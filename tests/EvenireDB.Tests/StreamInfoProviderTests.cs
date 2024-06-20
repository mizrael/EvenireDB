using EvenireDB.Common;

namespace EvenireDB.Tests;

public class StreamInfoProviderTests
{
    [Fact]
    public async Task DeleteStreamAsync_should_return_false_when_stream_does_not_exist()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var extentsProvider = Substitute.For<IExtentsProvider>();
        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        var result = await sut.DeleteStreamAsync(streamId);
        result.Should().BeFalse();

        var exists = sut.GetStreamInfo(streamId) is not null;
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetStreamsInfo_should_return_empty_when_no_streams_available()
    {
        var extentsProvider = Substitute.For<IExtentsProvider>();
        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        var results = sut.GetStreamsInfo();
        results.Should().BeEmpty();
    }

    [Fact]
    public void GetStreamInfo_should_return_null_when_stream_does_not_exist()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var extentsProvider = Substitute.For<IExtentsProvider>();
        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        sut.GetStreamInfo(streamId).Should().BeNull();
    }
}