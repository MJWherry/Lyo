using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary>One node in a lazy folder tree backed by <see cref="IFileTreeSource" />.</summary>
public sealed class FileTreeNode
{
    /// <summary>Builds a node for <paramref name="entry" />.</summary>
    public FileTreeNode(FileSystemEntry entry)
    {
        ArgumentHelpers.ThrowIfNull(entry);
        Entry = entry;
    }

    /// <summary>Listing row for this node.</summary>
    public FileSystemEntry Entry { get; }

    /// <summary>True after <see cref="FileTreeLoader.EnsureChildrenAsync" /> has run.</summary>
    public bool ChildrenLoaded { get; set; }

    /// <summary>Loaded children. Empty until loaded; directories may still be expandable.</summary>
    public List<FileTreeNode> Children { get; } = [];
}

/// <summary>Loads children for <see cref="FileTreeNode" /> from an <see cref="IFileTreeSource" />.</summary>
public static class FileTreeLoader
{
    /// <summary>Root directory node for <paramref name="path" />.</summary>
    public static FileTreeNode CreateRoot(string path, string name = "/")
        => new(new(path, name, true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue));

    /// <summary>Lists <paramref name="node" /> once via <paramref name="source" />.</summary>
    public static async Task EnsureChildrenAsync(IFileTreeSource source, FileTreeNode node, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(node);
        if (node.ChildrenLoaded)
            return;

        var listed = await source.ListChildrenAsync(node.Entry.Path, ct).ConfigureAwait(false);
        node.Children.Clear();
        foreach (var entry in listed)
            node.Children.Add(new(entry));
        node.ChildrenLoaded = true;
    }

    /// <summary>
    /// Loads every descendant directory under <paramref name="node" />. Stops after <paramref name="fetchCap" /> folder listings that were not already loaded.
    /// </summary>
    public static async Task<(int Fetches, bool Truncated)> EnsureDescendantsAsync(
        IFileTreeSource source,
        FileTreeNode node,
        int fetchCap,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(node);
        ArgumentHelpers.ThrowIfLessThan(fetchCap, 1);
        var fetches = 0;
        var truncated = await EnsureDescendantsCoreAsync(source, node, fetchCap, () => fetches, v => fetches = v, ct).ConfigureAwait(false);
        return (fetches, truncated);
    }

    private static async Task<bool> EnsureDescendantsCoreAsync(
        IFileTreeSource source,
        FileTreeNode node,
        int fetchCap,
        Func<int> getFetches,
        Action<int> setFetches,
        CancellationToken ct)
    {
        if (!node.Entry.IsDirectory)
            return false;

        if (!node.ChildrenLoaded) {
            if (getFetches() >= fetchCap)
                return true;

            await EnsureChildrenAsync(source, node, ct).ConfigureAwait(false);
            setFetches(getFetches() + 1);
        }

        var truncated = false;
        foreach (var child in node.Children)
            truncated |= await EnsureDescendantsCoreAsync(source, child, fetchCap, getFetches, setFetches, ct).ConfigureAwait(false);

        return truncated;
    }
}
