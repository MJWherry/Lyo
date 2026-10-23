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
        var root = FileTreeLoader.CreateRoot(fs.RootPath);
        await FileTreeLoader.EnsureChildrenAsync(source, root, TestCt);
        Assert.True(root.ChildrenLoaded);
        Assert.Contains(root.Children, n => n.Entry.Name == "reports");
        Assert.Contains(root.Children, n => n.Entry.Name == "a.bin");
    }

    [Fact]
    public async Task EnsureDescendants_LoadsNestedFileForSearch()
    {
        using var fs = new MemoryFileSystem();
        fs.CreateDirectory(fs.RootPath + "/reports");
        fs.WriteAllBytes(fs.RootPath + "/reports/keep.txt", "x"u8.ToArray());
        var source = new FileSystemTreeSource(fs);
        var root = FileTreeLoader.CreateRoot(fs.RootPath);
        await FileTreeLoader.EnsureChildrenAsync(source, root, TestCt);
        var reports = Assert.Single(root.Children, n => n.Entry.Name == "reports");
        Assert.False(reports.ChildrenLoaded);
        var (_, truncated) = await FileTreeLoader.EnsureDescendantsAsync(source, root, 200, TestCt);
        Assert.False(truncated);
        Assert.Contains(reports.Children, n => n.Entry.Name == "keep.txt");
    }
}