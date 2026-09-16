using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage;

/// <summary>Resolves a physical object key to a jailed <see cref="IFileSystem" /> path. Hosts that must not reference <see cref="IFileSystem" /> should call the read/mutate helpers instead.</summary>
public static class FileStoragePhysicalPath
{
    /// <summary>Normalizes <paramref name="physicalKey" /> and combines it under the backend jail. Does not require the file to exist.</summary>
    public static (IFileSystem FileSystem, string FullPath, string Key) Resolve(IFileStorageService fileStorage, string physicalKey)
    {
        ArgumentHelpers.ThrowIfNull(fileStorage);
        var key = FileHelpers.NormalizeAndValidatePathPrefix(physicalKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (fileStorage is not IFileStoragePhysical listing)
            throw new InvalidOperationException("This storage backend cannot use physical keys.");

        var fs = listing.Physical;
        return (fs, ToFullPath(fs, key), key);
    }

    /// <summary>Same as <see cref="Resolve" /> but throws <see cref="FileNotFoundException" /> when the object is missing.</summary>
    public static async Task<(IFileSystem FileSystem, string FullPath, string Key)> ResolveExistingAsync(
        IFileStorageService fileStorage,
        string physicalKey,
        CancellationToken ct = default)
    {
        var resolved = Resolve(fileStorage, physicalKey);
        if (!await resolved.FileSystem.FileExistsAsync(resolved.FullPath, ct).ConfigureAwait(false))
            throw new FileNotFoundException($"Physical object not found: {resolved.Key}", resolved.Key);

        return resolved;
    }

    /// <summary>Last segment of a posix key.</summary>
    public static string Leaf(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var posix = key.Replace('\\', '/').Trim('/');
        var slash = posix.LastIndexOf('/');
        return slash < 0 ? posix : posix.Substring(slash + 1);
    }

    /// <summary>Parent prefix of a posix key, or null at the root.</summary>
    public static string? ParentKey(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var posix = key.Replace('\\', '/').Trim('/');
        var slash = posix.LastIndexOf('/');
        return slash < 0 ? null : posix.Substring(0, slash);
    }

    /// <summary>Single-segment file name. Rejects slashes, <c>..</c>, and NULs.</summary>
    public static string NormalizeFileName(string fileName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName);
        var trimmed = fileName.Trim();
        if (trimmed.IndexOf('/') >= 0 || trimmed.IndexOf('\\') >= 0 || trimmed.IndexOf('\0') >= 0
            || trimmed.IndexOf("..", StringComparison.Ordinal) >= 0)
            throw new ArgumentException("File name must be a single path segment.", nameof(fileName));

        return trimmed;
    }

    /// <summary>Joins an optional prefix and a leaf into a relative physical key.</summary>
    public static string JoinKey(string? pathPrefix, string fileName)
    {
        var leaf = NormalizeFileName(fileName);
        var prefix = FileHelpers.NormalizeAndValidatePathPrefix(pathPrefix);
        return string.IsNullOrEmpty(prefix) ? leaf : prefix + "/" + leaf;
    }

    /// <summary>Combines <paramref name="key" /> under <paramref name="fileSystem" />'s root jail.</summary>
    public static string ToFullPath(IFileSystem fileSystem, string key)
    {
        ArgumentHelpers.ThrowIfNull(fileSystem);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var parts = key.Replace('\\', '/').Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var segments = new string[parts.Length + 1];
        segments[0] = fileSystem.RootPath;
        Array.Copy(parts, 0, segments, 1, parts.Length);
        return PathHelpers.Combine(fileSystem.PathStyle, segments);
    }
}
