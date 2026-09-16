using Lyo.Common.Core.Identifiers;
using Lyo.Common.Core.Pathing;

namespace Lyo.IO.FileSystem.Tests;

public sealed class LocalFileSystemTests : IFileSystemContractTests
{
    protected override ValueTask<IFileSystem?> CreateAsync() => new(NewFileSystem());

    [Fact]
    public void PathStyle_IsHost()
    {
        using var fs = NewFileSystem();
        Assert.Equal(PathStyle.Host, fs.PathStyle);
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsAsyncCapableStream()
    {
        using var fs = NewFileSystem();
        var path = Path.Combine(fs.RootPath, "async-read.bin");
        fs.WriteAllBytes(path, "payload"u8.ToArray());

        await using var stream = await fs.OpenReadAsync(path, TestCt);
        Assert.True(Assert.IsType<FileStream>(stream, exactMatch: false).IsAsync);
    }

    [Fact]
    public async Task OpenCreateAsync_ReturnsAsyncCapableStream()
    {
        using var fs = NewFileSystem();
        await using var stream = await fs.OpenCreateAsync(Path.Combine(fs.RootPath, "async-write.bin"), TestCt);
        Assert.True(Assert.IsType<FileStream>(stream, exactMatch: false).IsAsync);
    }

    [Fact]
    public async Task OpenAppendAsync_ReturnsAsyncCapableStream()
    {
        using var fs = NewFileSystem();
        await using var stream = await fs.OpenAppendAsync(Path.Combine(fs.RootPath, "async-append.bin"), TestCt);
        Assert.True(Assert.IsType<FileStream>(stream, exactMatch: false).IsAsync);
    }

    [Fact]
    public void Constructor_CreateRootIfMissingFalse_DoesNotCreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-vfs-norootcreate-" + LyoGuid.CreateV7().ToString("N"));
        using var fs = new LocalFileSystem(new LocalFileSystemOptions { RootPath = root, CreateRootIfMissing = false });
        Assert.False(Directory.Exists(fs.RootPath));
    }

    private static LocalFileSystem NewFileSystem()
        => new(Path.Combine(Path.GetTempPath(), "lyo-vfs-local-" + LyoGuid.CreateV7().ToString("N")));
}
