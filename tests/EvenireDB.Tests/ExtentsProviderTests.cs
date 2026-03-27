using EvenireDB.Common;

namespace EvenireDB.Tests;

public class ExtentsProviderTests : IClassFixture<DataFixture>
{
    private readonly DataFixture _fixture;

    public ExtentsProviderTests(DataFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void GetExtentInfo_should_return_null_when_stream_does_not_exist()
    {
        var config = _fixture.CreateExtentsConfig();
        var sut = new ExtentsProvider(config);
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var result = sut.GetExtentInfo(streamId);

        Assert.Null(result);
    }

    [Fact]
    public void GetExtentInfo_should_return_extent_when_createIfMissing_is_true()
    {
        var config = _fixture.CreateExtentsConfig();
        var sut = new ExtentsProvider(config);
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var result = sut.GetExtentInfo(streamId, createIfMissing: true);

        Assert.NotNull(result);
        Assert.Equal(streamId, result.StreamId);
        Assert.Contains(streamId.Key.ToString("N"), result.HeadersPath);
        Assert.Contains(streamId.Key.ToString("N"), result.DataPath);
        Assert.Contains("lorem", result.HeadersPath);
    }

    [Fact]
    public void GetExtentInfo_should_create_type_subdirectory()
    {
        var config = _fixture.CreateExtentsConfig();
        var sut = new ExtentsProvider(config);
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "orders" };

        sut.GetExtentInfo(streamId, createIfMissing: true);

        var typedDir = Path.Combine(config.BasePath, "orders");
        Assert.True(Directory.Exists(typedDir));
    }

    [Fact]
    public void GetAllExtentsInfo_should_return_empty_when_no_streams_exist()
    {
        var config = _fixture.CreateExtentsConfig();
        var sut = new ExtentsProvider(config);

        var results = sut.GetAllExtentsInfo().ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void GetAllExtentsInfo_should_discover_persisted_streams()
    {
        var config = _fixture.CreateExtentsConfig();
        var sut = new ExtentsProvider(config);
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var extent = sut.GetExtentInfo(streamId, createIfMissing: true)!;
        File.WriteAllBytes(extent.HeadersPath, Array.Empty<byte>());

        var results = sut.GetAllExtentsInfo().ToList();

        Assert.Single(results);
        Assert.Equal(streamId, results[0].StreamId);
    }

    [Fact]
    public void GetAllExtentsInfo_should_filter_by_stream_type()
    {
        var config = _fixture.CreateExtentsConfig();
        var sut = new ExtentsProvider(config);

        var ordersStream = new StreamId { Key = Guid.NewGuid(), Type = "orders" };
        var usersStream = new StreamId { Key = Guid.NewGuid(), Type = "users" };

        var ext1 = sut.GetExtentInfo(ordersStream, createIfMissing: true)!;
        File.WriteAllBytes(ext1.HeadersPath, Array.Empty<byte>());
        var ext2 = sut.GetExtentInfo(usersStream, createIfMissing: true)!;
        File.WriteAllBytes(ext2.HeadersPath, Array.Empty<byte>());

        var ordersOnly = sut.GetAllExtentsInfo(streamType: "orders").ToList();

        Assert.Single(ordersOnly);
        Assert.Equal(ordersStream, ordersOnly[0].StreamId);
    }

    [Fact]
    public async Task DeleteExtentsAsync_should_remove_files()
    {
        var config = _fixture.CreateExtentsConfig();
        var sut = new ExtentsProvider(config);
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        var extent = sut.GetExtentInfo(streamId, createIfMissing: true)!;
        await File.WriteAllBytesAsync(extent.HeadersPath, new byte[] { 0x01 });
        await File.WriteAllBytesAsync(extent.DataPath, new byte[] { 0x02 });

        await sut.DeleteExtentsAsync(streamId);

        Assert.False(File.Exists(extent.HeadersPath));
        Assert.False(File.Exists(extent.DataPath));
    }

    [Fact]
    public async Task DeleteExtentsAsync_should_noop_when_stream_does_not_exist()
    {
        var config = _fixture.CreateExtentsConfig();
        var sut = new ExtentsProvider(config);
        var streamId = new StreamId { Key = Guid.NewGuid(), Type = "lorem" };

        // Should not throw
        await sut.DeleteExtentsAsync(streamId);
    }
}
