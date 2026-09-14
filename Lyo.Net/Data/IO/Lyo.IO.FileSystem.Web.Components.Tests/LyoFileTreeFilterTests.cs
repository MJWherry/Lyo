using Lyo.IO.FileSystem;
using MudBlazor;

namespace Lyo.IO.FileSystem.Web.Components.Tests;

public sealed class LyoFileTreeFilterTests
{
    [Fact]
    public void Apply_EmptyFilter_ReturnsAll()
    {
        var items = new List<TreeItemData<FileTreeNode>> { Item("a.bin", "/a.bin") };
        Assert.Same(items, LyoFileTree.FileTreeFilter.Apply(items, "   "));
    }

    [Fact]
    public void Apply_KeepsAncestorWhenChildMatches()
    {
        var child = Item("keep.txt", "/reports/keep.txt");
        var folder = Item("reports", "/reports", child);
        var kept = LyoFileTree.FileTreeFilter.Apply([folder], "keep");
        var root = Assert.Single(kept);
        Assert.Equal("reports", root.Text);
        Assert.True(root.Expanded);
        Assert.Equal("keep.txt", Assert.Single(root.Children!).Text);
    }

    [Fact]
    public void Apply_DropsNonMatchingBranch()
    {
        var items = new List<TreeItemData<FileTreeNode>> { Item("skip.bin", "/skip.bin") };
        Assert.Empty(LyoFileTree.FileTreeFilter.Apply(items, "keep"));
    }

    private static TreeItemData<FileTreeNode> Item(string name, string path, params TreeItemData<FileTreeNode>[] children)
    {
        var node = new FileTreeNode(new FileSystemEntry(path, name, children.Length > 0, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue));
        return new TreeItemData<FileTreeNode> {
            Value = node,
            Text = name,
            Children = children.Length == 0 ? null : [.. children]
        };
    }
}
