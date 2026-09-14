using Lyo.FileMetadataStore;

namespace Lyo.FileStorage;

/// <summary>Catalog path strings: <c>/{prefix}/{fileId:D}</c> or <c>/{fileId:D}</c> at root. Not an <c>IFileSystem</c>.</summary>
public static class FileStorageLogicalPath
{
    /// <summary>Builds the logical file path for <paramref name="fileId" /> under <paramref name="pathPrefix" />.</summary>
    public static string For(string? pathPrefix, Guid fileId)
    {
        var prefix = FileMetadataPathPrefix.Normalize(pathPrefix);
        return prefix == null ? "/" + fileId.ToString("D") : "/" + prefix + "/" + fileId.ToString("D");
    }

    /// <summary>Directory VFS path for a PathPrefix. Root is <c>/</c>.</summary>
    public static string Directory(string? pathPrefix)
    {
        var prefix = FileMetadataPathPrefix.Normalize(pathPrefix);
        return prefix == null ? "/" : "/" + prefix;
    }
}
