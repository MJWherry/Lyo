using System.Diagnostics;
using Lyo.Common.Metadata.Records;
using Lyo.Compression.Models;
using Lyo.Encryption;

namespace Lyo.FileMetadataStore.Models;

/// <summary>
/// Stored file metadata. <see cref="Charset" /> is the client-declared character encoding of the plaintext (IANA/web name such as <c>utf-8</c> or <c>windows-1252</c>), stored as
/// given after trim. Not converted. Null when omitted.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public record FileStoreResult(
    Guid Id,
    string? OriginalFileName,
    long OriginalFileSize,
    byte[] OriginalFileHash,
    string SourceFileName,
    long SourceFileSize,
    byte[] SourceFileHash,
    bool IsCompressed,
    CompressionAlgorithm? CompressionAlgorithm,
    long? CompressedFileSize,
    byte[]? CompressedFileHash,
    bool IsEncrypted,
    EncryptionAlgorithm? DataEncryptionKeyAlgorithm,
    EncryptionAlgorithm? KeyEncryptionKeyAlgorithm,
    long? EncryptedFileSize,
    byte[]? EncryptedFileHash,
    byte[]? EncryptedDataEncryptionKey,
    string? DataEncryptionKeyId,
    string? DataEncryptionKeyVersion,
    byte[]? KeyEncryptionKeySalt,
    DateTime Timestamp,
    string? PathPrefix = null,
    HashAlgorithm? HashAlgorithm = null,
    string? ContentType = null,
    string? Charset = null,
    string? TenantId = null,
    FileAvailability Availability = FileAvailability.Available,
    byte? DekKeyMaterialBytes = null,
    DateTime? DeletedAt = null,
    Guid? OwnerId = null)
{
    /// <summary>Maximum stored length for <see cref="Charset" /> (matches the metadata column).</summary>
    public const int MaxCharsetLength = 64;

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"{Id} {OriginalFileName} {FileSizeUnitInfo.FormatBestFitAbbreviation(OriginalFileSize)}{(IsCompressed ? " Compressed" : "")}{(IsEncrypted ? " Encrypted" : "")}";
}