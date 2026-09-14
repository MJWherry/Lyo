using Lyo.FileStorage.Web.Components.FileStorageManagement;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileStoragePathTreeBuilderTests
{
    [Fact]
    public void MergeImmediateChildren_Root_PlacesNullPrefixFilesAndFirstSegmentFolders()
    {
        var root = FileStoragePathTreeBuilder.CreateRoot();
        var rootFileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var nestedFileId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        FileStoragePathTreeRow[] rows = [
            new(rootFileId, null, "readme.txt", 12, false),
            new(nestedFileId, "reports/2024", "out.pdf", 99, false)
        ];

        FileStoragePathTreeBuilder.MergeImmediateChildren(root, rows, rows.Select(r => r.PathPrefix).ToList(), truncated: false);

        Assert.True(root.ChildrenLoaded);
        Assert.Equal(2, root.Children.Count);
        Assert.True(root.Children[0].IsDirectory);
        Assert.Equal("reports", root.Children[0].Name);
        Assert.False(root.Children[0].ChildrenLoaded);
        Assert.False(root.Children[1].IsDirectory);
        Assert.Equal(rootFileId, root.Children[1].FileId);
        Assert.Equal("readme.txt", root.Children[1].Name);
    }

    [Fact]
    public void CombineWithActive_NullExtra_MatchesGridTombstoneFilter()
    {
        var grid = FileStorageGridRowHelper.CreateActiveFilesWhere();
        var combined = FileStorageGridRowHelper.CombineWithActive(null);
        Assert.Equal(grid, combined);
    }

    [Fact]
    public void CreatePendingChild_IsNotARealFile()
    {
        var root = FileStoragePathTreeBuilder.CreateRoot();
        var pending = FileStoragePathTreeBuilder.CreatePendingChild(root);
        Assert.True(FileStoragePathTreeBuilder.IsPendingChild(pending));
        Assert.False(pending.IsDirectory);
        Assert.Null(pending.FileId);
    }

    [Fact]
    public void PathTreeNodeKeyComparer_Null_DoesNotThrow()
    {
        var comparer = FileStoragePathTreeNodeKeyComparer.Instance;
        Assert.True(comparer.Equals(null, null));
        Assert.Equal(0, comparer.GetHashCode(null!));
    }

    [Fact]
    public void CreateRoot_ReplacesPreviousRoot_FindReturnsNewInstance()
    {
        var previous = FileStoragePathTreeBuilder.CreateRoot();
        previous.ChildrenLoaded = true;
        var next = FileStoragePathTreeBuilder.CreateRoot();
        Assert.NotSame(previous, next);
        Assert.False(next.ChildrenLoaded);
        Assert.Same(next, FileStoragePathTreeBuilder.Find(next, previous.Key));
    }

    [Fact]
    public void StorageKeyExists_ExactAndSuffix_Match()
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var expected = FileStorageGridRowHelper.BuildExpectedStorageKey(fileId, fileId.ToString("N") + ".csv", "report");
        Assert.True(FileStorageGridRowHelper.StorageKeyExists([expected], expected));
        Assert.True(FileStorageGridRowHelper.StorageKeyExists(["tenant/" + expected], expected));
        Assert.False(FileStorageGridRowHelper.StorageKeyExists(["other/" + fileId.ToString("N")], expected));
    }

    [Fact]
    public void FlattenVisible_CollapsedFolder_OmitsChildren()
    {
        var root = FileStoragePathTreeBuilder.CreateRoot();
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        FileStoragePathTreeBuilder.MergeImmediateChildren(root, [new(fileId, null, "a.txt", 1, false)], [null], truncated: false);
        List<TreeItemData<FileStoragePathTreeNode>> items = [
            new() {
                Value = root,
                Expandable = true,
                Expanded = false,
                Children = root.Children.Select(static c => new TreeItemData<FileStoragePathTreeNode> { Value = c }).ToList()
            }
        ];

        var rows = FileStoragePathTreeBuilder.FlattenVisible(items);
        Assert.Single(rows);
        Assert.Equal(root.Key, rows[0].Node.Key);
        Assert.Equal(0, rows[0].Depth);
        Assert.False(rows[0].Expanded);
    }

    [Fact]
    public void FlattenVisible_Expanded_IncludesChildrenAtNextDepth()
    {
        var root = FileStoragePathTreeBuilder.CreateRoot();
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        FileStoragePathTreeBuilder.MergeImmediateChildren(root, [new(fileId, null, "a.txt", 1, false)], [null], truncated: false);
        List<TreeItemData<FileStoragePathTreeNode>> items = [
            new() {
                Value = root,
                Expandable = true,
                Expanded = true,
                Children = root.Children.Select(static c => new TreeItemData<FileStoragePathTreeNode> { Value = c, Expandable = false }).ToList()
            }
        ];

        var rows = FileStoragePathTreeBuilder.FlattenVisible(items);
        Assert.Equal(2, rows.Count);
        Assert.Equal(root.Key, rows[0].Node.Key);
        Assert.True(rows[0].Expanded);
        Assert.Equal(fileId, rows[1].Node.FileId);
        Assert.Equal(1, rows[1].Depth);
    }
}
