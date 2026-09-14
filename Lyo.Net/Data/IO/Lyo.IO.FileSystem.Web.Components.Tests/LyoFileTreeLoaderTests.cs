using Lyo.IO.FileSystem;

namespace Lyo.IO.FileSystem.Web.Components.Tests;

public sealed class LyoFileTreeLoaderTests
{
    [Fact]
    public async Task EnsureChildren_MemorySource_LoadsImmediateOnly()
    {
        using var fs = new MemoryFileSystem();
        fs.CreateDirectory(fs.RootPath + "/reports");
        fs.WriteAllBytes(fs.RootPath + "/a.bin", "x"u8.ToArray());
        var source = new FileSystemTreeSource(fs);
        var root = FileTreeLoader.CreateRoot(fs.RootPath, "/");
        await FileTreeLoader.EnsureChildrenAsync(source, root, TestContext.Current.CancellationToken);
        Assert.True(root.ChildrenLoaded);
        Assert.Contains(root.Children, n => n.Entry.Name == "reports");
        Assert.Contains(root.Children, n => n.Entry.Name == "a.bin");
    }
}
