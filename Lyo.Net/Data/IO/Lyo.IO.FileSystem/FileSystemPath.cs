using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

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

    /// <summary>
    /// Resolves <paramref name="path" /> under <paramref name="normalizedRoot" /> and returns the normalized full path. A relative path resolves against the root; an absolute
    /// path is used as-is. Throws <see cref="InvalidFormatException" /> when the result escapes the root.
    /// </summary>
    /// <remarks>
    /// <paramref name="normalizedRoot" /> must already be the output of <see cref="NormalizeRoot" />. This is the hot path — every <see cref="IFileSystem" /> operation calls it
    /// at least once — so it normalizes exactly one path per call.
    /// </remarks>
    public static string Jail(PathStyle style, string normalizedRoot, string path)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        var full = PathHelpers.GetFullPath(style, PathHelpers.IsPathRooted(style, path) ? path : PathHelpers.Combine(style, normalizedRoot, path));
        if (!PathHelpers.IsUnderNormalizedRoot(style, normalizedRoot, full))
            throw new InvalidFormatException($"Path escapes root '{normalizedRoot}': {path}", nameof(path), path, $"Path under root {normalizedRoot}");

        return full;
    }
}