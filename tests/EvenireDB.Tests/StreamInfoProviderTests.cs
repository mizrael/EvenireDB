using EvenireDB.Common;
using System.Runtime.InteropServices;

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
        Assert.False(result);

        var exists = sut.GetStreamInfo(streamId) is not null;
        Assert.False(exists);
    }

    [Fact]
    public void GetStreamsInfo_should_return_empty_when_no_streams_available()
    {
        var extentsProvider = Substitute.For<IExtentsProvider>();
        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        var results = sut.GetStreamsInfo();
        Assert.Empty(results);
    }

    [Fact]
    public void GetStreamInfo_should_return_null_when_stream_does_not_exist()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var extentsProvider = Substitute.For<IExtentsProvider>();
        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        Assert.Null(sut.GetStreamInfo(streamId));
    }

    [Fact]
    public void GetStreamInfo_should_return_info_when_stream_exists()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var headersPath = Path.GetTempFileName();

        try
        {
            var headerSize = Marshal.SizeOf<RawHeader>();
            File.WriteAllBytes(headersPath, new byte[headerSize * 5]);

            var extent = new ExtentInfo(streamId, "data.dat", headersPath);
            var extentsProvider = Substitute.For<IExtentsProvider>();
            extentsProvider.GetExtentInfo(streamId, false).Returns(extent);

            var cache = Substitute.For<IStreamsCache>();
            cache.Contains(streamId).Returns(true);

            var logger = Substitute.For<ILogger<StreamInfoProvider>>();
            var sut = new StreamInfoProvider(extentsProvider, cache, logger);

            var result = sut.GetStreamInfo(streamId);

            Assert.NotNull(result);
            Assert.Equal(streamId, result.Id);
            Assert.Equal(5, result.EventsCount);
            Assert.True(result.IsCached);
        }
        finally
        {
            File.Delete(headersPath);
        }
    }

    [Fact]
    public async Task DeleteStreamAsync_should_return_true_and_remove_from_cache_when_stream_exists()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var extent = new ExtentInfo(streamId, "data.dat", "headers.dat");

        var extentsProvider = Substitute.For<IExtentsProvider>();
        extentsProvider.GetExtentInfo(streamId, createIfMissing: false).Returns(extent);

        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        var result = await sut.DeleteStreamAsync(streamId);

        Assert.True(result);
        await extentsProvider.Received(1).DeleteExtentsAsync(streamId, Arg.Any<CancellationToken>());
        cache.Received(1).Remove(streamId);
    }

    [Fact]
    public async Task DeleteStreamAsync_should_return_false_when_extent_deletion_throws()
    {
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };
        var extent = new ExtentInfo(streamId, "data.dat", "headers.dat");

        var extentsProvider = Substitute.For<IExtentsProvider>();
        extentsProvider.GetExtentInfo(streamId, createIfMissing: false).Returns(extent);
        extentsProvider.DeleteExtentsAsync(streamId, Arg.Any<CancellationToken>())
            .Returns<ValueTask>(x => throw new InvalidOperationException("disk error"));

        var cache = Substitute.For<IStreamsCache>();
        var logger = Substitute.For<ILogger<StreamInfoProvider>>();
        var sut = new StreamInfoProvider(extentsProvider, cache, logger);

        var result = await sut.DeleteStreamAsync(streamId);

        Assert.False(result);
        cache.DidNotReceive().Remove(Arg.Any<StreamId>());
    }

    [Fact]
    public void GetStreamsInfo_should_return_info_for_all_extents()
    {
        var stream1 = new StreamId { Key = Guid.NewGuid(), Type = "orders" };
        var stream2 = new StreamId { Key = Guid.NewGuid(), Type = "users" };

        var headersPath1 = Path.GetTempFileName();
        var headersPath2 = Path.GetTempFileName();

        try
        {
            var headerSize = Marshal.SizeOf<RawHeader>();
            File.WriteAllBytes(headersPath1, new byte[headerSize * 3]);
            File.WriteAllBytes(headersPath2, new byte[headerSize * 7]);

            var ext1 = new ExtentInfo(stream1, "data1.dat", headersPath1);
            var ext2 = new ExtentInfo(stream2, "data2.dat", headersPath2);

            var extentsProvider = Substitute.For<IExtentsProvider>();
            extentsProvider.GetAllExtentsInfo(null).Returns(new[] { ext1, ext2 });
            extentsProvider.GetExtentInfo(stream1, false).Returns(ext1);
            extentsProvider.GetExtentInfo(stream2, false).Returns(ext2);

            var cache = Substitute.For<IStreamsCache>();
            var logger = Substitute.For<ILogger<StreamInfoProvider>>();
            var sut = new StreamInfoProvider(extentsProvider, cache, logger);

            var results = sut.GetStreamsInfo().ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal(3, results.First(r => r.Id == stream1).EventsCount);
            Assert.Equal(7, results.First(r => r.Id == stream2).EventsCount);
        }
        finally
        {
            File.Delete(headersPath1);
            File.Delete(headersPath2);
        }
    }
}