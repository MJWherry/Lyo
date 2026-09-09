using Lyo.FileSystemWatcher.Models;
using Lyo.Testing;

namespace Lyo.FileSystemWatcher.Tests;

public class FileSystemSnapshotDifferTests
{
    [Fact]
    public void DetectChanges_NewFile_IsCreated()
    {
        var oldTree = Tree("root", File("a.txt", 1, "h1"));
        var newTree = Tree("root", File("a.txt", 1, "h1"), File("b.txt", 2, "h2"));
        var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, newTree, ct: TestContext.Current.CancellationToken);
        var created = changes.Single(c => !c.IsDirectory);
        created.ChangeType.ShouldBe(FileSystemChangeKind.Created);
        created.NewPath.ShouldBe("b.txt");
        created.OldPath.ShouldBeNull();
    }

    [Fact]
    public void DetectChanges_DeletedFile_IsDeleted()
    {
        var oldTree = Tree("root", File("a.txt", 1, "h1"));
        var newTree = Tree("root");
        var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, newTree, ct: TestContext.Current.CancellationToken);
        var deleted = changes.Single(c => !c.IsDirectory);
        deleted.ChangeType.ShouldBe(FileSystemChangeKind.Deleted);
        deleted.OldPath.ShouldBe("a.txt");
    }

    [Fact]
    public void DetectChanges_HashChange_IsChanged()
    {
        var oldTree = Tree("root", File("a.txt", 1, "h1", "fp1"));
        var newTree = Tree("root", File("a.txt", 2, "h2", "fp2"));
        var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, newTree, ct: TestContext.Current.CancellationToken);
        changes.Single().ChangeType.ShouldBe(FileSystemChangeKind.Changed);
    }

    [Fact]
    public void DetectChanges_SameHashDifferentPathSameParent_IsRenamed()
    {
        var oldTree = Tree("root", File("a.txt", 10, "same-hash"));
        var newTree = Tree("root", File("b.txt", 10, "same-hash"));
        var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, newTree, ct: TestContext.Current.CancellationToken);
        var renamed = changes.Single(c => !c.IsDirectory);
        renamed.ChangeType.ShouldBe(FileSystemChangeKind.Renamed);
        renamed.OldPath.ShouldBe("a.txt");
        renamed.NewPath.ShouldBe("b.txt");
    }

    [Fact]
    public void DetectChanges_SameHashDifferentParent_IsMoved()
    {
        var oldTree = Tree("root", File("dir1/a.txt", 10, "same-hash"));
        AddDir(oldTree, "dir2");
        var newTree = Tree("root", File("dir2/a.txt", 10, "same-hash"));
        AddDir(newTree, "dir1");
        var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, newTree, ct: TestContext.Current.CancellationToken);
        var moved = changes.Single(c => !c.IsDirectory);
        moved.ChangeType.ShouldBe(FileSystemChangeKind.Moved);
        moved.OldPath.ShouldBe("dir1/a.txt");
        moved.NewPath.ShouldBe("dir2/a.txt");
    }

    [Fact]
    public void DetectChanges_RoundTripJson_PreservesChanges()
    {
        var oldTree = Tree("root", File("a.txt", 1, "h1"));
        var newTree = Tree("root", File("a.txt", 1, "h1"), File("b.txt", 2, "h2"));
        var json = System.Text.Json.JsonSerializer.Serialize(newTree);
        var restored = System.Text.Json.JsonSerializer.Deserialize<FileSystemSnapshotTreeDto>(json);
        restored.ShouldNotBeNull();
        var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, restored!, ct: TestContext.Current.CancellationToken);
        changes.Single(c => !c.IsDirectory).ChangeType.ShouldBe(FileSystemChangeKind.Created);
    }

    private static FileSystemSnapshotEntryDto File(string path, long size, string hash, string? fingerprint = null)
        => new() { Path = path, IsDirectory = false, FileSize = size, Hash = hash, Fingerprint = fingerprint ?? hash };

    private static FileSystemSnapshotTreeDto Tree(string root, params FileSystemSnapshotEntryDto[] files)
    {
        var tree = new FileSystemSnapshotTreeDto {
            RootPath = root,
            PathComparison = nameof(StringComparison.OrdinalIgnoreCase),
            Root = new() { RelativePath = "" }
        };
        foreach (var file in files)
            PlaceFile(tree, file.Path, file);

        Recount(tree);
        return tree;
    }

    private static void AddDir(FileSystemSnapshotTreeDto tree, string relativePath)
    {
        tree.TryGetDirectory("", out var current);
        foreach (var segment in relativePath.Split('/')) {
            if (!current!.Directories.TryGetValue(segment, out var next)) {
                var childPath = string.IsNullOrEmpty(current.RelativePath) ? segment : current.RelativePath + "/" + segment;
                next = new() { RelativePath = childPath };
                current.Directories[segment] = next;
            }

            current = next;
        }
    }

    private static void PlaceFile(FileSystemSnapshotTreeDto tree, string relativePath, FileSystemSnapshotEntryDto file)
    {
        var slash = relativePath.LastIndexOf('/');
        if (slash >= 0)
            AddDir(tree, relativePath.Substring(0, slash));

        tree.TryGetParentForPlace(relativePath, out var dir);
        var name = slash < 0 ? relativePath : relativePath.Substring(slash + 1);
        dir!.Files[name] = file;
        Recount(tree);
    }

    private static void Recount(FileSystemSnapshotTreeDto tree)
    {
        var files = 0;
        var dirs = 0;
        Count(tree.Root, ref files, ref dirs);
        tree.FileCount = files;
        tree.DirectoryCount = dirs;
    }

    private static void Count(FileSystemSnapshotDirectoryDto node, ref int files, ref int dirs)
    {
        files += node.Files.Count;
        foreach (var sub in node.Directories.Values) {
            dirs++;
            Count(sub, ref files, ref dirs);
        }
    }
}

file static class FileSystemSnapshotTreeDtoTestExtensions
{
    public static bool TryGetParentForPlace(this FileSystemSnapshotTreeDto tree, string relativePath, out FileSystemSnapshotDirectoryDto? dir)
    {
        var slash = relativePath.LastIndexOf('/');
        if (slash < 0)
            return tree.TryGetDirectory("", out dir);

        return tree.TryGetDirectory(relativePath.Substring(0, slash), out dir);
    }
}
