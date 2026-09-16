using Lyo.IO.FileSystem;

namespace Lyo.IO.FileSystem.Tests;

public sealed class FileSystemTreeSourceTests
{
    [Fact]
    public async Task ListChildren_MemoryFileSystem_ReturnsImmediateEntries()
    {
        using var fs = new MemoryFileSystem();
        await fs.CreateDirectoryAsync(fs.RootPath + "/reports", TestCt);
        fs.WriteAllBytes(fs.RootPath + "/a.bin", "x"u8.ToArray());
        fs.WriteAllBytes(fs.RootPath + "/reports/b.bin", "y"u8.ToArray());
        var source = new FileSystemTreeSource(fs);
        var root = FileTreeLoader.CreateRoot(fs.RootPath);
        await FileTreeLoader.EnsureChildrenAsync(source, root, TestCt);
        Assert.Contains(root.Children, n => n.Entry is { IsDirectory: true, Name: "reports" });
        Assert.Contains(root.Children, n => n.Entry is { IsDirectory: false, Name: "a.bin" });
        var reports = Assert.Single(root.Children, n => n.Entry.IsDirectory);
        await FileTreeLoader.EnsureChildrenAsync(source, reports, TestCt);
        Assert.Contains(reports.Children, n => n.Entry.Name == "b.bin");
        Assert.DoesNotContain(root.Children, n => n.Entry.Name == "b.bin");
    }
}
