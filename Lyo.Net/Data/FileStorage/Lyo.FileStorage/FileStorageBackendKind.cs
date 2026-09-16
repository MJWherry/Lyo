using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage;

/// <summary>Which object-store implementation backs <see cref="IFileStoragePhysical.Physical" />. Integers match the API DTO.</summary>
public enum FileStorageBackendKind
{
    /// <summary>No physical backend (metadata-only host).</summary>
    None = 0,

    /// <summary>Local disk jail.</summary>
    Local = 1,

    /// <summary>Amazon S3 (or S3-compatible) object store.</summary>
    AwsS3 = 2,

    /// <summary>Azure Blob container.</summary>
    AzureBlob = 3,

    /// <summary>Another <see cref="Lyo.IO.FileSystem.IFileSystem" /> implementation.</summary>
    Other = 4
}

/// <summary>Detects <see cref="FileStorageBackendKind" /> from a storage service without referencing vendor packages.</summary>
public static class FileStorageBackend
{
    /// <summary>Maps <see cref="IFileStoragePhysical.Physical" />'s runtime type name to a kind.</summary>
    public static FileStorageBackendKind Detect(IFileStorageService? fileStorage)
    {
        if (fileStorage is not IFileStoragePhysical listing)
            return FileStorageBackendKind.None;

        return listing.Physical.GetType().Name switch {
            "LocalFileSystem" => FileStorageBackendKind.Local,
            "S3FileSystem" => FileStorageBackendKind.AwsS3,
            "AzureBlobFileSystem" => FileStorageBackendKind.AzureBlob,
            var _ => FileStorageBackendKind.Other
        };
    }

    /// <summary>Short label for chips and confirmations.</summary>
    public static string DisplayName(FileStorageBackendKind kind)
        => kind switch {
            FileStorageBackendKind.Local => "Local",
            FileStorageBackendKind.AwsS3 => "S3",
            FileStorageBackendKind.AzureBlob => "Azure Blob",
            FileStorageBackendKind.Other => "object store",
            var _ => "storage"
        };
}
