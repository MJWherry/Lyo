using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Web.Components.DataGrid;

namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

/// <summary>
/// One node in the workbench path tree. Directories are virtual <c>PathPrefix</c> segments; files are metadata rows. Empty prefixes live under the root node
/// (<see cref="FileStoragePathTreeBuilder.RootDisplayName" />).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FileStoragePathTreeNode
{
    /// <summary>Stable key: <c>dir:</c> for root, <c>dir:{prefix}</c> for folders, <c>file:{id}</c> for files.</summary>
    public required string Key { get; init; }

    /// <summary>Folder segment or original file name shown in the tree.</summary>
    public required string Name { get; init; }

    /// <summary>True for a virtual folder (including pending folders that do not yet have files).</summary>
    public bool IsDirectory { get; init; }

    /// <summary>Folder prefix (null at root). For files, the file's <c>PathPrefix</c>.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Metadata id when <see cref="IsDirectory" /> is false.</summary>
    public Guid? FileId { get; init; }

    /// <summary>True when the metadata row is a tombstone.</summary>
    public bool IsDeleted { get; init; }

    /// <summary>Original size from the projected row, when known.</summary>
    public long? OriginalFileSize { get; init; }

    /// <summary>Client-only folder created with New folder; not persisted until a file is uploaded with this prefix.</summary>
    public bool IsPending { get; set; }

    /// <summary>True after QueryProject children have been merged into <see cref="Children" />.</summary>
    public bool ChildrenLoaded { get; set; }

    /// <summary>True when paging stopped before QueryProject reported no more rows.</summary>
    public bool Truncated { get; set; }

    /// <summary>Immediate child folders and files.</summary>
    public List<FileStoragePathTreeNode> Children { get; } = [];

    /// <inheritdoc />
    public override string ToString()
        => IsDirectory
            ? $"FileStoragePathTreeNode: dir {Name} prefix={PathPrefix ?? "(root)"} children={Children.Count}{(IsPending ? " pending" : "")}"
            : $"FileStoragePathTreeNode: file {Name} FileId={FileId}{(IsDeleted ? " deleted" : "")}";
}

/// <summary>Projected FileMetadata fields needed to build tree children.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FileStoragePathTreeRow(Guid FileId, string? PathPrefix, string? OriginalFileName, long OriginalFileSize, bool IsDeleted)
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileStoragePathTreeRow: FileId={FileId}, OriginalFileName={OriginalFileName ?? "(none)"}, PathPrefix={PathPrefix ?? "(none)"}{(IsDeleted ? " deleted" : "")}";
}

/// <summary>Builds and updates <see cref="FileStoragePathTreeNode" /> graphs from PathPrefix rows. Does not call the API.</summary>
public static class FileStoragePathTreeBuilder
{
    /// <summary>Label for the virtual root (files with no path prefix).</summary>
    public const string RootDisplayName = "(root)";

    /// <summary>QueryProject page size when loading a folder.</summary>
    public const int PageSize = 500;

    /// <summary>Max QueryProject pages per load before setting <see cref="FileStoragePathTreeNode.Truncated" />.</summary>
    public const int MaxPages = 10;

    /// <summary>Creates an unloaded root node.</summary>
    public static FileStoragePathTreeNode CreateRoot()
        => new() {
            Key = DirectoryKey(null),
            Name = RootDisplayName,
            IsDirectory = true,
            PathPrefix = null
        };

    /// <summary>Directory key for a prefix. Root is <c>dir:</c>.</summary>
    public static string DirectoryKey(string? pathPrefix)
    {
        var normalized = Normalize(pathPrefix);
        return normalized == null ? "dir:" : $"dir:{normalized}";
    }

    /// <summary>File key for a metadata id.</summary>
    public static string FileKey(Guid fileId) => $"file:{fileId:D}";

    /// <summary>Trims slashes and maps backslashes to forward slashes. Empty becomes null.</summary>
    public static string? Normalize(string? pathPrefix)
    {
        var trimmed = FileHelpers.NormalizePathPrefix(pathPrefix);
        if (string.IsNullOrEmpty(trimmed))
            return null;

        return trimmed.Replace('\\', '/');
    }

    /// <summary>Splits a normalized prefix into path segments.</summary>
    public static IReadOnlyList<string> SplitSegments(string? pathPrefix)
    {
        var normalized = Normalize(pathPrefix);
        return normalized == null ? [] : normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Finds a node by <see cref="FileStoragePathTreeNode.Key" /> under <paramref name="root" /> (inclusive).</summary>
    public static FileStoragePathTreeNode? Find(FileStoragePathTreeNode root, string key)
    {
        ArgumentHelpers.ThrowIfNull(root);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (root.Key == key)
            return root;

        foreach (var child in root.Children) {
            var match = Find(child, key);
            if (match != null)
                return match;
        }

        return null;
    }

    /// <summary>Finds the directory node for <paramref name="pathPrefix" /> under <paramref name="root" />.</summary>
    public static FileStoragePathTreeNode? FindDirectory(FileStoragePathTreeNode root, string? pathPrefix) => Find(root, DirectoryKey(pathPrefix));

    /// <summary>
    /// Builds a complete folder/file tree from metadata rows. Each <c>PathPrefix</c> segment becomes a directory; each row becomes a file leaf under that prefix
    /// (or under root when the prefix is empty).
    /// </summary>
    public static FileStoragePathTreeNode BuildFullTree(IReadOnlyList<FileStoragePathTreeRow> rows, bool truncated)
    {
        ArgumentHelpers.ThrowIfNull(rows);
        var root = CreateRoot();
        root.ChildrenLoaded = true;
        root.Truncated = truncated;
        var dirs = new Dictionary<string, FileStoragePathTreeNode>(StringComparer.Ordinal) { [DirectoryKey(null)] = root };

        foreach (var row in rows) {
            var parent = root;
            foreach (var segment in SplitSegments(row.PathPrefix)) {
                var prefix = parent.PathPrefix == null ? segment : $"{parent.PathPrefix}/{segment}";
                var key = DirectoryKey(prefix);
                if (!dirs.TryGetValue(key, out var dir)) {
                    dir = CreateDirectoryNode(prefix);
                    dir.ChildrenLoaded = true;
                    parent.Children.Add(dir);
                    dirs[key] = dir;
                }

                parent = dir;
            }

            parent.Children.Add(CreateFileNode(row));
        }

        SortChildrenRecursive(root);
        return root;
    }

    /// <summary>
    /// Replaces <paramref name="parent" /> immediate children from <paramref name="rows" />. Files whose prefix equals the parent are file leaves; longer prefixes contribute
    /// the next path segment as a folder. Pending folders that are not yet represented in <paramref name="rows" /> are kept.
    /// </summary>
    public static void MergeImmediateChildren(FileStoragePathTreeNode parent, IReadOnlyList<FileStoragePathTreeRow> rows, bool truncated)
    {
        ArgumentHelpers.ThrowIfNull(parent);
        ArgumentHelpers.ThrowIfNull(rows);

        var pending = parent.Children.Where(static c => c.IsPending && c.IsDirectory).ToList();
        var folders = new Dictionary<string, FileStoragePathTreeNode>(StringComparer.Ordinal);
        var files = new List<FileStoragePathTreeNode>();

        foreach (var row in rows) {
            var prefix = Normalize(row.PathPrefix);
            if (IsImmediateFile(parent.PathPrefix, prefix)) {
                files.Add(CreateFileNode(row));
                continue;
            }

            var childPrefix = ImmediateChildFolderPrefix(parent.PathPrefix, prefix);
            if (childPrefix == null || folders.ContainsKey(childPrefix))
                continue;

            folders[childPrefix] = CreateDirectoryNode(childPrefix);
        }

        parent.Children.Clear();
        foreach (var folder in pending) {
            var pendingPrefix = Normalize(folder.PathPrefix);
            if (pendingPrefix != null && !folders.ContainsKey(pendingPrefix))
                parent.Children.Add(folder);
        }

        foreach (var folder in folders.Values.OrderBy(static n => n.Name, StringComparer.OrdinalIgnoreCase))
            parent.Children.Add(folder);

        foreach (var file in files.OrderBy(static n => n.Name, StringComparer.OrdinalIgnoreCase))
            parent.Children.Add(file);

        parent.ChildrenLoaded = true;
        parent.Truncated = truncated;
    }

    private static void SortChildrenRecursive(FileStoragePathTreeNode node)
    {
        if (node.Children.Count == 0)
            return;

        node.Children.Sort(static (a, b) => {
            if (a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? -1 : 1;

            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
        foreach (var child in node.Children) {
            if (child.IsDirectory)
                SortChildrenRecursive(child);
        }
    }

    /// <summary>
    /// Adds a client-only child folder under <paramref name="parent" />. <paramref name="name" /> must be a single path segment. Returns the existing folder when the prefix is
    /// already present.
    /// </summary>
    public static FileStoragePathTreeNode AddPendingFolder(FileStoragePathTreeNode parent, string name)
    {
        ArgumentHelpers.ThrowIfNull(parent);
        var segment = Normalize(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(segment);
        if (segment.Contains('/') || segment.Contains('\\'))
            throw new ArgumentException("Folder name must be a single path segment.", nameof(name));

        FileHelpers.ThrowIfPathPrefixTraversal(segment, nameof(name));
        var prefix = parent.PathPrefix == null ? segment : $"{parent.PathPrefix}/{segment}";
        var existing = parent.Children.FirstOrDefault(c => c.IsDirectory && string.Equals(Normalize(c.PathPrefix), prefix, StringComparison.Ordinal));
        if (existing != null)
            return existing;

        var node = CreateDirectoryNode(prefix);
        node.IsPending = true;
        node.ChildrenLoaded = true;
        var insertAt = parent.Children.TakeWhile(c => c.IsDirectory && string.Compare(c.Name, node.Name, StringComparison.OrdinalIgnoreCase) <= 0).Count();
        parent.Children.Insert(insertAt, node);
        return node;
    }

    /// <summary>
    /// Active (non-deleted) file ids under <paramref name="nodes" />. A directory contributes every descendant file, so bulk DEK / delete / move on a folder covers its contents.
    /// </summary>
    public static IReadOnlyList<Guid> CollectActiveFileIds(IEnumerable<FileStoragePathTreeNode> nodes)
    {
        ArgumentHelpers.ThrowIfNull(nodes);
        var ids = new HashSet<Guid>();
        foreach (var node in nodes)
            CollectActiveFileIds(node, ids);

        return [.. ids];
    }

    /// <summary>
    /// Builds per-file destinations for dropping <paramref name="selected" /> onto <paramref name="target" />. Individual files land in the target prefix. Directories keep their folder
    /// name and relative child prefixes. Returns an empty list when the drop would move a folder into itself or is a no-op.
    /// </summary>
    public static IReadOnlyList<(Guid FileId, string? NewPathPrefix)> CollectMovesToDirectory(
        IReadOnlyList<FileStoragePathTreeNode> selected, FileStoragePathTreeNode target)
    {
        ArgumentHelpers.ThrowIfNull(selected);
        ArgumentHelpers.ThrowIfNull(target);
        if (!target.IsDirectory)
            return [];

        var targetPrefix = Normalize(target.PathPrefix);
        foreach (var node in selected) {
            if (!node.IsDirectory)
                continue;
            if (node.Key == target.Key)
                return [];
            if (node.PathPrefix != null && IsStrictlyUnderPrefix(node.PathPrefix, targetPrefix))
                return [];
        }

        var selectedDirs = selected.Where(static n => n.IsDirectory).ToList();
        var covered = new HashSet<Guid>();
        var moves = new List<(Guid FileId, string? NewPathPrefix)>();

        foreach (var dir in selectedDirs) {
            foreach (var file in EnumerateActiveFiles(dir)) {
                if (file.FileId is not { } id || !covered.Add(id))
                    continue;

                var relative = RelativePrefix(dir.PathPrefix, file.PathPrefix);
                var newPrefix = dir.PathPrefix == null ? JoinPrefixes(targetPrefix, relative) : JoinPrefixes(targetPrefix, dir.Name, relative);
                if (string.Equals(Normalize(file.PathPrefix), Normalize(newPrefix), StringComparison.Ordinal))
                    continue;

                moves.Add((id, newPrefix));
            }
        }

        foreach (var file in selected) {
            if (file.IsDirectory || file.FileId is not { } id || file.IsDeleted || !covered.Add(id))
                continue;
            if (selectedDirs.Any(d => IsUnderDirectory(file, d)))
                continue;
            if (string.Equals(Normalize(file.PathPrefix), targetPrefix, StringComparison.Ordinal))
                continue;

            moves.Add((id, targetPrefix));
        }

        return moves;
    }

    /// <summary>Maps a projected QueryProject row when <c>Id</c> can be parsed.</summary>
    public static bool TryReadRow(object? row, out FileStoragePathTreeRow parsed)
    {
        parsed = default;
        if (!FileStorageGridRowHelper.TryGetFileIdFromRow(row, out var fileId))
            return false;

        var sizeRaw = ProjectedValueHelper.GetValue(row, "OriginalFileSize");
        ProjectedValueHelper.TryGetInt64(sizeRaw, out var size);
        parsed = new(
            fileId, FileStorageGridRowHelper.GetPathPrefixFromRow(row), FileStorageGridRowHelper.GetOriginalFileNameFromRow(row), size,
            FileStorageGridRowHelper.IsRowDeleted(row));
        return true;
    }

    internal static bool IsImmediateFile(string? parentPrefix, string? filePrefix)
        => string.Equals(Normalize(parentPrefix), Normalize(filePrefix), StringComparison.Ordinal);

    internal static string? ImmediateChildFolderPrefix(string? parentPrefix, string? filePrefix)
    {
        var parent = Normalize(parentPrefix);
        var file = Normalize(filePrefix);
        if (file == null)
            return null;

        if (parent == null) {
            var slash = file.IndexOf('/');
            return slash < 0 ? file : file[..slash];
        }

        var expected = parent + "/";
        if (!file.StartsWith(expected, StringComparison.Ordinal))
            return null;

        var rest = file[expected.Length..];
        if (rest.Length == 0)
            return null;

        var nextSlash = rest.IndexOf('/');
        var segment = nextSlash < 0 ? rest : rest[..nextSlash];
        return $"{parent}/{segment}";
    }

    internal static bool IsStrictlyUnderPrefix(string? ancestor, string? descendant)
    {
        var parent = Normalize(ancestor);
        var child = Normalize(descendant);
        return parent != null && child != null && child.StartsWith(parent + "/", StringComparison.Ordinal);
    }

    private static void CollectActiveFileIds(FileStoragePathTreeNode node, HashSet<Guid> ids)
    {
        if (!node.IsDirectory) {
            if (node is { FileId: { } id, IsDeleted: false })
                ids.Add(id);
            return;
        }

        foreach (var child in node.Children)
            CollectActiveFileIds(child, ids);
    }

    private static IEnumerable<FileStoragePathTreeNode> EnumerateActiveFiles(FileStoragePathTreeNode node)
    {
        if (!node.IsDirectory) {
            if (node is { FileId: not null, IsDeleted: false })
                yield return node;
            yield break;
        }

        foreach (var child in node.Children) {
            foreach (var file in EnumerateActiveFiles(child))
                yield return file;
        }
    }

    private static bool IsUnderDirectory(FileStoragePathTreeNode file, FileStoragePathTreeNode dir)
    {
        if (dir.PathPrefix == null)
            return true;

        var filePrefix = Normalize(file.PathPrefix);
        var dirPrefix = Normalize(dir.PathPrefix);
        return string.Equals(filePrefix, dirPrefix, StringComparison.Ordinal) || IsStrictlyUnderPrefix(dirPrefix, filePrefix);
    }

    private static string? RelativePrefix(string? ancestor, string? prefix)
    {
        var parent = Normalize(ancestor);
        var child = Normalize(prefix);
        if (parent == null)
            return child;
        if (child == null)
            return null;
        if (string.Equals(parent, child, StringComparison.Ordinal))
            return null;

        var expected = parent + "/";
        return child.StartsWith(expected, StringComparison.Ordinal) ? child[expected.Length..] : child;
    }

    private static string? JoinPrefixes(params string?[] parts)
    {
        List<string> segments = [];
        foreach (var part in parts) {
            var normalized = Normalize(part);
            if (normalized != null)
                segments.Add(normalized);
        }

        return segments.Count == 0 ? null : string.Join('/', segments);
    }

    private static FileStoragePathTreeNode CreateDirectoryNode(string pathPrefix)
    {
        var segments = SplitSegments(pathPrefix);
        return new() {
            Key = DirectoryKey(pathPrefix),
            Name = segments.Count == 0 ? RootDisplayName : segments[^1],
            IsDirectory = true,
            PathPrefix = pathPrefix
        };
    }

    private static FileStoragePathTreeNode CreateFileNode(FileStoragePathTreeRow row)
    {
        var name = string.IsNullOrWhiteSpace(row.OriginalFileName) ? row.FileId.ToString("D") : row.OriginalFileName;
        return new() {
            Key = FileKey(row.FileId),
            Name = name,
            IsDirectory = false,
            PathPrefix = Normalize(row.PathPrefix),
            FileId = row.FileId,
            IsDeleted = row.IsDeleted,
            OriginalFileSize = row.OriginalFileSize,
            ChildrenLoaded = true
        };
    }
}

/// <summary>Drag-and-drop payload for the path tree: checked (or dragged) sources onto a directory.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FileStoragePathTreeDrop(IReadOnlyList<FileStoragePathTreeNode> Sources, FileStoragePathTreeNode Target)
{
    /// <inheritdoc />
    public override string ToString() => $"FileStoragePathTreeDrop: {Sources.Count} source(s) -> {Target.Name}";
}

/// <summary>Compares tree nodes by <see cref="FileStoragePathTreeNode.Key" /> so MudTreeView multi-select survives item rebuilds.</summary>
public sealed class FileStoragePathTreeNodeKeyComparer : IEqualityComparer<FileStoragePathTreeNode>
{
    /// <summary>Shared instance.</summary>
    public static readonly FileStoragePathTreeNodeKeyComparer Instance = new();

    /// <inheritdoc />
    public bool Equals(FileStoragePathTreeNode? x, FileStoragePathTreeNode? y)
        => ReferenceEquals(x, y) || (x is not null && y is not null && string.Equals(x.Key, y.Key, StringComparison.Ordinal));

    /// <inheritdoc />
    public int GetHashCode(FileStoragePathTreeNode obj)
    {
        ArgumentHelpers.ThrowIfNull(obj);
        return StringComparer.Ordinal.GetHashCode(obj.Key);
    }
}
