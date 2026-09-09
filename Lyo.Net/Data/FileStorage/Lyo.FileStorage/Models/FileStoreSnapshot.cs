using System.Diagnostics;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Models;

/// <summary>
/// Redacted view of <see cref="FileStoreResult" /> that is safe to raise on public events. Drops <see cref="FileStoreResult.EncryptedDataEncryptionKey" />,
/// <see cref="FileStoreResult.KeyEncryptionKeySalt" />, and raw file hashes, which event subscribers should not see by default. Call <see cref="From" /> at the
/// event-publication boundary.
/// </summary>
/// <param name="Id">File id.</param>
/// <param name="OriginalFileName">Filename given at save time.</param>
/// <param name="OriginalFileSize">Original byte length.</param>
/// <param name="SourceFileSize">Stored byte length after compression and encryption.</param>
/// <param name="IsCompressed">True if the persisted object is compressed.</param>
/// <param name="IsEncrypted">True if the persisted object is encrypted.</param>
/// <param name="ContentType">Resolved MIME type.</param>
/// <param name="Charset">Client-declared character encoding of the plaintext. Null when omitted.</param>
/// <param name="TenantId">Tenant id. Already treated as non-sensitive at the storage layer.</param>
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
    string? Charset,
    string? TenantId,
    FileAvailability Availability,
    string? PathPrefix,
    DateTime Timestamp)
{
    /// <summary>Builds a redacted snapshot from a complete <see cref="FileStoreResult" />.</summary>
    public static FileStoreSnapshot From(FileStoreResult m)
        => new(
            m.Id, m.OriginalFileName ?? m.Id.ToString(), m.OriginalFileSize, m.SourceFileSize, m.IsCompressed, m.IsEncrypted, m.ContentType, m.Charset, m.TenantId, m.Availability,
            m.PathPrefix, m.Timestamp);

    /// <inheritdoc />
    public override string ToString()
        => $"FileStoreSnapshot: {Id} {OriginalFileName} size={OriginalFileSize}{(IsCompressed ? " Compressed" : "")}{(IsEncrypted ? " Encrypted" : "")} {Availability}";
}