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
    string? PhysicalKey)
{
    /// <inheritdoc />
    public override string ToString()
        => IsDirectory ? $"dir {Name} prefix={PathPrefix}" : $"file {Name} FileId={FileId} {Presence}";
}

/// <summary>Immediate children of one PathPrefix.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorageFolderListResponse(IReadOnlyList<FileStorageFolderEntryDto> Entries, bool Truncated)
{
    /// <inheritdoc />
    public override string ToString() => $"FileStorageFolderListResponse: {Entries.Count} entries truncated={Truncated}";
}
