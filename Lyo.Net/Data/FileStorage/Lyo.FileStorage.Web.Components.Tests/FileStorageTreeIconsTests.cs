using Lyo.FileStorage.Web.Components.FileStorageManagement;
using Lyo.IO.FileSystem;
using Lyo.IO.FileSystem.Web.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileStorageTreeIconsTests
{
    [Fact]
    public void Resolve_Directory_IsFolder()
    {
        var node = new FileTreeNode(new FileSystemEntry("/reports", "reports", true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue));
        Assert.Equal(Icons.Material.Filled.Folder, FileStorageTreeIcons.Resolve(node));
    }

    [Fact]
    public void Resolve_Csv_IsTable()
    {
        var node = new FileTreeNode(new FileSystemEntry("/a.csv", "a.csv", false, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue));
        Assert.Equal(Icons.Material.Filled.TableChart, FileStorageTreeIcons.Resolve(node));
    }

    [Fact]
    public void Resolve_Png_IsImage()
    {
        var node = new FileTreeNode(
            new FileSystemEntry(
                "/a", "a", false, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
                new Dictionary<string, string> { ["OriginalFileName"] = "photo.png" }));
        Assert.Equal(Icons.Material.Filled.Image, FileStorageTreeIcons.Resolve(node));
    }
}
