using System.Diagnostics;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Models;

/// <summary>
/// Redacted projection of <see cref="FileStoreResult" /> safe to emit on public events. Excludes <see cref="FileStoreResult.EncryptedDataEncryptionKey" />,
/// <see cref="FileStoreResult.KeyEncryptionKeySalt" />, and raw file hashes — fields an event subscriber typically should not see by default. Use <see cref="From" /> at the
/// event-publication boundary.
/// </summary>
/// <param name="Id">File identifier.</param>
/// <param name="OriginalFileName">Filename supplied at save time.</param>
/// <param name="OriginalFileSize">Original byte length.</param>
/// <param name="SourceFileSize">Stored byte length (post-compression / -encryption).</param>
/// <param name="IsCompressed">Whether the persisted object is compressed.</param>
/// <param name="IsEncrypted">Whether the persisted object is encrypted.</param>
/// <param name="ContentType">Resolved MIME type.</param>
/// <param name="TenantId">Tenant identifier (already non-sensitive at the storage layer).</param>
/// <param name="Availability">Current availability state.</param>
/// <param name="PathPrefix">Logical storage prefix used to namespace the object.</param>
/// <param name="Timestamp">Last write timestamp.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStoreSnapshot(
    Guid Id,
    string OriginalFileName,
    long OriginalFileSize,
    long SourceFileSize,
    bool IsCompressed,
    bool IsEncrypted,
    string? ContentType,
    string? TenantId,
    FileAvailability Availability,
    string? PathPrefix,
    DateTime Timestamp)
{
    /// <summary>Builds a redacted snapshot from a full <see cref="FileStoreResult" />.</summary>
    public static FileStoreSnapshot From(FileStoreResult m)
        => new(
            m.Id, m.OriginalFileName ?? m.Id.ToString(), m.OriginalFileSize, m.SourceFileSize, m.IsCompressed, m.IsEncrypted, m.ContentType, m.TenantId, m.Availability,
            m.PathPrefix, m.Timestamp);

    /// <inheritdoc />
    public override string ToString()
        => $"FileStoreSnapshot: {Id} {OriginalFileName} size={OriginalFileSize}{(IsCompressed ? " Compressed" : "")}{(IsEncrypted ? " Encrypted" : "")} {Availability}";
}