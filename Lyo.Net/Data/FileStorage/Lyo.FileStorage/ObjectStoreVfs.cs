using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Pathing;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage;

/// <summary>Maps POSIX VFS paths onto object-store keys (no leading slash on the key). Shared by S3 and Azure Blob adapters.</summary>
public static class ObjectStoreVfs
{
    /// <summary>Turns an optional key/blob prefix into a POSIX <see cref="IFileSystem.RootPath" /> (<c>/</c> when empty).</summary>
    public static string NormalizeRoot(string? keyPrefix)
    {
        if (keyPrefix.IsNullOrWhitespace())
            return "/";

        var trimmed = keyPrefix.Trim().Trim('/');
        return FileSystemPath.NormalizeRoot(PathStyle.Posix, "/" + trimmed);
    }

    /// <summary>Object key for <paramref name="jailedPosixPath" /> (leading slash stripped). Root <c>/</c> is an empty key.</summary>
    public static string ToObjectKey(string jailedPosixPath)
    {
        if (jailedPosixPath.IsNullOrEmpty() || jailedPosixPath == "/")
            return "";

        return jailedPosixPath.TrimStart('/');
    }

    /// <summary>POSIX VFS path for an object key.</summary>
    public static string ToVfsPath(string objectKey)
    {
        if (objectKey.IsNullOrEmpty())
            return "/";

        return objectKey[0] == '/' ? objectKey : "/" + objectKey;
    }

    /// <summary>List/create prefix for a directory, always with a trailing slash except for the bucket/container root.</summary>
    public static string DirectoryPrefix(string jailedPosixPath)
    {
        var key = ToObjectKey(jailedPosixPath);
        if (key.Length == 0)
            return "";

        return key.EndsWith("/") ? key : key + "/";
    }
}
