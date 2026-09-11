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
    public void StorageKeyJoin_ExactAndSuffix_Match()
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var expected = FileStorageGridRowHelper.BuildExpectedStorageKey(fileId, fileId.ToString("N") + ".csv", "report");
        Assert.True(FileStorageStorageKeyJoin.KeyExists([expected], expected));
        Assert.True(FileStorageStorageKeyJoin.KeyExists(["tenant/" + expected], expected));
        Assert.False(FileStorageStorageKeyJoin.KeyExists(["other/" + fileId.ToString("N")], expected));
    }

    [Fact]
    public void ApplyPresence_BothMissingAndUnknown()
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var source = fileId.ToString("N") + ".csv";
        FileStoragePathTreeRow[] rows = [new(fileId, "report", "a.csv", 1, false, source)];
        var expected = FileStorageGridRowHelper.BuildExpectedStorageKey(fileId, source, "report");

        var both = FileStorageStorageKeyJoin.ApplyPresence(rows, [expected]);
        Assert.Equal(FileStoragePresence.Both, both[0].Presence);

        var missing = FileStorageStorageKeyJoin.ApplyPresence(rows, ["unrelated/key"]);
        Assert.Equal(FileStoragePresence.MissingBlob, missing[0].Presence);

        var unknown = FileStorageStorageKeyJoin.ApplyPresence(rows, null);
        Assert.Equal(FileStoragePresence.Unknown, unknown[0].Presence);
    }

    [Fact]
    public void ApplyPresence_DeletedRow_StillReportsBlobPresence()
    {
        var fileId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var source = fileId.ToString("N");
        FileStoragePathTreeRow[] rows = [new(fileId, "report", "gone.csv", 1, true, source)];
        var expected = FileStorageGridRowHelper.BuildExpectedStorageKey(fileId, source, "report");
        var applied = FileStorageStorageKeyJoin.ApplyPresence(rows, [expected]);
        Assert.True(applied[0].IsDeleted);
        Assert.Equal(FileStoragePresence.Both, applied[0].Presence);
    }

    [Fact]
    public void TryParseStorageKey_ShardAndPrefixed()
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var n = fileId.ToString("N");
        Assert.True(FileStorageStorageKeyJoin.TryParseStorageKey($"{n[..2]}/{n.Substring(2, 2)}/{n}.enc", out var shardId, out var shardPrefix, out var shardName));
        Assert.Equal(fileId, shardId);
        Assert.Null(shardPrefix);
        Assert.Equal(n + ".enc", shardName);

        Assert.True(FileStorageStorageKeyJoin.TryParseStorageKey($"report/{n}.csv", out var prefixedId, out var prefix, out _));
        Assert.Equal(fileId, prefixedId);
        Assert.Equal("report", prefix);
    }

    [Fact]
    public void AddCloudOnlyOrphans_ImmediateFileAndChildFolder()
    {
        var root = FileStoragePathTreeBuilder.CreateRoot();
        FileStoragePathTreeBuilder.MergeImmediateChildren(root, [], [], truncated: false);
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var nestedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var n = fileId.ToString("N");
        var nestedN = nestedId.ToString("N");
        FileStoragePathTreeBuilder.AddCloudOnlyOrphans(
            root, [$"{n[..2]}/{n.Substring(2, 2)}/{n}", $"trees/{nestedN}"]);

        Assert.Contains(root.Children, c => c is { IsDirectory: true, Name: "trees" });
        Assert.Contains(root.Children, c => c is { FileId: var id, Presence: FileStoragePresence.CloudOnly } && id == fileId);
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
