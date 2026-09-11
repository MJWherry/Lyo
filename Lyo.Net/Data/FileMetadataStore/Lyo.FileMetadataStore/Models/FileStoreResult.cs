using System.Diagnostics;
using System.Text.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Compression.Models;
using Lyo.Encryption;

namespace Lyo.FileMetadataStore.Models;

/// <summary>
/// Stored file metadata. <see cref="Charset" /> is the client-declared character encoding of the plaintext (IANA/web name such as <c>utf-8</c> or <c>windows-1252</c>), stored as
/// given after trim. Not converted. Null when omitted. <see cref="Metadata" /> is an opaque caller JSON bag; Lyo does not interpret keys.
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
    Guid? OwnerId = null,
    JsonElement? Metadata = null)
{
    /// <summary>Maximum stored length for <see cref="Charset" /> (matches the metadata column).</summary>
    public const int MaxCharsetLength = 64;

    /// <summary>Writes <see cref="Metadata" /> for jsonb/TEXT persistence. Null, undefined, and JSON null become a null column.</summary>
    /// <param name="metadata">Caller JSON bag, or null.</param>
    /// <returns>Raw JSON text, or null when there is nothing to store.</returns>
    public static string? SerializeMetadata(JsonElement? metadata)
    {
        if (metadata is not { } el)
            return null;

        return el.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? null : el.GetRawText();
    }

    /// <summary>Parses a stored JSON column into <see cref="Metadata" />. Blank input is null.</summary>
    /// <param name="json">Stored column text.</param>
    /// <returns>A cloned element the caller can keep after the parser disposes, or null.</returns>
    public static JsonElement? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"{Id} {OriginalFileName} {FileSizeUnitInfo.FormatBestFitAbbreviation(OriginalFileSize)}{(IsCompressed ? " Compressed" : "")}{(IsEncrypted ? " Encrypted" : "")}";
}