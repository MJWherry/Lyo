using Lyo.Common.Core.Pathing;

namespace Lyo.IO.FileSystem.Tests;

public sealed class MemoryFileSystemTests : IFileSystemContractTests
{
    protected override IFileSystem Create() => new MemoryFileSystem();

    [Fact]
    public void PathStyle_IsPosix()
    {
        using var fs = Create();
        Assert.Equal(PathStyle.Posix, fs.PathStyle);
    }

    [Fact]
    public void RootPath_IsUniquePerInstance()
    {
        using var a = new MemoryFileSystem();
        using var b = new MemoryFileSystem();
        Assert.NotEqual(a.RootPath, b.RootPath);
    }
}
