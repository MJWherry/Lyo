using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Records;
using Lyo.Compression.Models;
using Lyo.Encryption;

namespace Lyo.FileMetadataStore.Models;

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
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    CompressionAlgorithm? CompressionAlgorithm,
    long? CompressedFileSize,
    byte[]? CompressedFileHash,
    bool IsEncrypted,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    EncryptionAlgorithm? DataEncryptionKeyAlgorithm,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    EncryptionAlgorithm? KeyEncryptionKeyAlgorithm,
    long? EncryptedFileSize,
    byte[]? EncryptedFileHash,
    byte[]? EncryptedDataEncryptionKey,
    string? DataEncryptionKeyId,
    string? DataEncryptionKeyVersion,
    byte[]? KeyEncryptionKeySalt,
    DateTime Timestamp,
    string? PathPrefix = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    HashAlgorithm? HashAlgorithm = null,
    string? ContentType = null,
    string? TenantId = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    FileAvailability Availability = FileAvailability.Available,
    byte? DekKeyMaterialBytes = null)
{
    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"{Id} {OriginalFileName} {FileSizeUnitInfo.FormatBestFitAbbreviation(OriginalFileSize)}{(IsCompressed ? " Compressed" : "")}{(IsEncrypted ? " Encrypted" : "")}";
}