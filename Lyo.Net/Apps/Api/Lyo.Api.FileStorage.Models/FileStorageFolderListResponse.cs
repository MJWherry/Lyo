using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>Where a file is visible: metadata store, physical storage, or both. Integers match <c>Lyo.FileStorage.FileStoragePresence</c>.</summary>
[Flags]
public enum FileStoragePresence
{
    /// <summary>Unknown or unverified (physical listing was skipped).</summary>
    None = 0,

    /// <summary>A metadata row exists.</summary>
    Store = 1,

    /// <summary>A physical object exists.</summary>
    Physical = 2,

    /// <summary>Metadata row and a matching physical object.</summary>
    Both = Store | Physical
}

/// <summary>Which object-store implementation backs physical listing. Integers match <c>Lyo.FileStorage.FileStorageBackendKind</c>.</summary>
public enum FileStorageBackendKind
{
    /// <summary>No physical backend.</summary>
    None = 0,

    /// <summary>Local disk jail.</summary>
    Local = 1,

    /// <summary>Amazon S3 (or S3-compatible) object store.</summary>
    AwsS3 = 2,

    /// <summary>Azure Blob container.</summary>
    AzureBlob = 3,

    /// <summary>Another file-system implementation.</summary>
    Other = 4
}

/// <summary>One immediate child of a folder-list response.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorageFolderEntryDto(
    string Name,
    bool IsDirectory,
    string? PathPrefix,
    Guid? FileId,
    FileStoragePresence Presence,
    string? OriginalFileName,
    long OriginalFileSize,
    string? PhysicalKey,
    bool IsCompressed = false,
    string? Compression = null,
    bool IsEncrypted = false,
    string? Encryption = null)
{
    /// <inheritdoc />
    public override string ToString()
        => IsDirectory ? $"dir {Name} prefix={PathPrefix}" : $"file {Name} FileId={FileId} {Presence}";
}

/// <summary>Immediate children of one PathPrefix.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorageFolderListResponse(
    IReadOnlyList<FileStorageFolderEntryDto> Entries,
    bool Truncated,
    FileStorageBackendKind BackendKind = FileStorageBackendKind.None)
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileStorageFolderListResponse: {Entries.Count} entries truncated={Truncated} backend={BackendKind}";
}
