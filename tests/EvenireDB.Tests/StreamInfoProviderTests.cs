namespace EvenireDB.Tests;

public class StreamInfoProviderTests
{
    [Fact]
    public async Task DeleteStreamAsync_should_do_nothing_when_stream_does_not_exist()
    {
        var streamId = Guid.NewGuid();

        var extentsProvider = Substitute.For<IExtentInfoProvider>();
        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        await sut.DeleteStreamAsync(streamId);

        var exists = sut.GetStreamInfo(streamId) is not null;
        exists.Should().BeFalse();
    }

    [Fact]
    public void GetStreamInfo_should_return_null_when_stream_does_not_exist()
    {
        var streamId = Guid.NewGuid();

        var extentsProvider = Substitute.For<IExtentInfoProvider>();
        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        sut.GetStreamInfo(streamId).Should().BeNull();
    }
}