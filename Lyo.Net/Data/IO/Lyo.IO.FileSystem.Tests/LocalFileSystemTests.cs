using Lyo.Common.Core.Pathing;

namespace Lyo.IO.FileSystem.Tests;

public sealed class LocalFileSystemTests : IFileSystemContractTests
{
    protected override IFileSystem Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-vfs-local-" + Guid.NewGuid().ToString("N"));
        return new LocalFileSystem(root);
    }

    [Fact]
    public void PathStyle_IsHost()
    {
        using var fs = Create();
        Assert.Equal(PathStyle.Host, fs.PathStyle);
    }
}
