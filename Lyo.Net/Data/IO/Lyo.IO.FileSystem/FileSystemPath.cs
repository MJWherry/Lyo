using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary>Root jail helpers shared by <see cref="IFileSystem" /> implementations.</summary>
public static class FileSystemPath
{
    /// <summary>Normalizes <paramref name="root" /> and trims trailing separators.</summary>
    public static string NormalizeRoot(PathStyle style, string root)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(root);
        return PathHelpers.TrimTrailingSeparators(PathHelpers.GetFullPath(style, root), style);
    }

    /// <summary>Throws when <paramref name="path" /> escapes <paramref name="root" />, then returns the normalized full path.</summary>
    public static string Jail(PathStyle style, string root, string path)
    {
        PathHelpers.ThrowIfEscapesRoot(style, root, path);
        return PathHelpers.GetFullPath(style, path);
    }
}
