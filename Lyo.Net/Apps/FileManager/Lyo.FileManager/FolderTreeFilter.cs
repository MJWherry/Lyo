namespace Lyo.FileManager;

/// <summary>One loaded folder used by the tree filter. The window maps its tree onto this shape.</summary>
public sealed class FolderTreeNode
{
    /// <summary>Folder name shown in the tree.</summary>
    public required string Name { get; init; }

    /// <summary>Loaded children. An empty list is a folder that has not been expanded.</summary>
    public List<FolderTreeNode> Children { get; init; } = [];
}

/// <summary>Hides loaded branches whose names do not match. Does not walk the server.</summary>
public static class FolderTreeFilter
{
    /// <summary>Copies <paramref name="roots" />, keeping a node when its name matches or a loaded descendant matches. An empty query copies every node.</summary>
    public static IReadOnlyList<FolderTreeNode> Apply(IReadOnlyList<FolderTreeNode> roots, string? query)
    {
        var term = query?.Trim() ?? "";
        return roots.Select(root => Copy(root, term)).OfType<FolderTreeNode>().ToArray();
    }

    private static FolderTreeNode? Copy(FolderTreeNode node, string term)
    {
        var children = node.Children.Select(child => Copy(child, term)).OfType<FolderTreeNode>().ToList();
        var matches = term.Length == 0 || node.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || children.Count > 0;
        if (!matches)
            return null;

        return new() { Name = node.Name, Children = children };
    }
}