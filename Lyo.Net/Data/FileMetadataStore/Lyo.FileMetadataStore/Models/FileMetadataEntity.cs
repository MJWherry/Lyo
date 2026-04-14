using Lyo.Compression.Models;
using Lyo.Encryption;

namespace Lyo.FileMetadataStore.Models;

public sealed class FileMetadataEntity
{
    public string Id { get; set; } = null!;

    public string? OriginalFileName { get; set; }

    public long OriginalFileSize { get; set; }

    public byte[] OriginalFileHash { get; set; } = null!;

    public string SourceFileName { get; set; } = null!;

    public long SourceFileSize { get; set; }

    public byte[] SourceFileHash { get; set; } = null!;

    public bool IsCompressed { get; set; }

    public string? CompressionAlgorithm { get; set; }

    public long? CompressedFileSize { get; set; }

    public byte[]? CompressedFileHash { get; set; }

    public bool IsEncrypted { get; set; }

    public string? DataEncryptionKeyAlgorithm { get; set; }

    public string? KeyEncryptionKeyAlgorithm { get; set; }

    public long? EncryptedFileSize { get; set; }

    public byte[]? EncryptedFileHash { get; set; }

    public byte[]? EncryptedDataEncryptionKey { get; set; }

    public string? DataEncryptionKeyId { get; set; }

    public string? DataEncryptionKeyVersion { get; set; }

    public byte[]? KeyEncryptionKeySalt { get; set; }

    /// <summary>Plaintext DEK length in bytes (two-key stream header). Null for legacy rows.</summary>
    public byte? DekKeyMaterialBytes { get; set; }

    public DateTime Timestamp { get; set; }

    public string? PathPrefix { get; set; }

    /// <summary>Hash algorithm used when computing OriginalFileHash. Null for legacy files (treated as Sha256).</summary>
    public string? HashAlgorithm { get; set; }

    public string? ContentType { get; set; }

    public string? TenantId { get; set; }

    public string? Availability { get; set; }

    public FileStoreResult ToFileStoreResult()
    {
        CompressionAlgorithm? compressionAlgorithm = null;
        if (!string.IsNullOrEmpty(CompressionAlgorithm) && Enum.TryParse<CompressionAlgorithm>(CompressionAlgorithm, out var alg))
            compressionAlgorithm = alg;

        EncryptionAlgorithm? dekAlgorithm = null;
        if (!string.IsNullOrEmpty(DataEncryptionKeyAlgorithm) && Enum.TryParse<EncryptionAlgorithm>(DataEncryptionKeyAlgorithm, out var dekAlg))
            dekAlgorithm = dekAlg;

        EncryptionAlgorithm? kekAlgorithm = null;
        if (!string.IsNullOrEmpty(KeyEncryptionKeyAlgorithm) && Enum.TryParse<EncryptionAlgorithm>(KeyEncryptionKeyAlgorithm, out var kekAlg))
            kekAlgorithm = kekAlg;

        HashAlgorithm? hashAlgorithm = null;
        if (!string.IsNullOrEmpty(HashAlgorithm) && Enum.TryParse<HashAlgorithm>(HashAlgorithm, out var hashAlg))
            hashAlgorithm = hashAlg;

        var availability = FileAvailability.Available;
        if (!string.IsNullOrEmpty(Availability) && Enum.TryParse<FileAvailability>(Availability, out var av))
            availability = av;

        return new(
            Guid.Parse(Id), OriginalFileName, OriginalFileSize, OriginalFileHash, SourceFileName, SourceFileSize, SourceFileHash, IsCompressed, compressionAlgorithm,
            CompressedFileSize, CompressedFileHash, IsEncrypted, dekAlgorithm, kekAlgorithm, EncryptedFileSize, EncryptedFileHash, EncryptedDataEncryptionKey, DataEncryptionKeyId,
            DataEncryptionKeyVersion, KeyEncryptionKeySalt, Timestamp, PathPrefix, hashAlgorithm, ContentType, TenantId, availability, DekKeyMaterialBytes);
    }

    public static FileMetadataEntity FromFileStoreResult(FileStoreResult result)
        => new() {
            Id = result.Id.ToString(),
            OriginalFileName = result.OriginalFileName,
            OriginalFileSize = result.OriginalFileSize,
            OriginalFileHash = result.OriginalFileHash,
            SourceFileName = result.SourceFileName,
            SourceFileSize = result.SourceFileSize,
            SourceFileHash = result.SourceFileHash,
            IsCompressed = result.IsCompressed,
            CompressionAlgorithm = result.CompressionAlgorithm?.ToString(),
            CompressedFileSize = result.CompressedFileSize,
            CompressedFileHash = result.CompressedFileHash,
            IsEncrypted = result.IsEncrypted,
            DataEncryptionKeyAlgorithm = result.DataEncryptionKeyAlgorithm?.ToString(),
            KeyEncryptionKeyAlgorithm = result.KeyEncryptionKeyAlgorithm?.ToString(),
            EncryptedFileSize = result.EncryptedFileSize,
            EncryptedFileHash = result.EncryptedFileHash,
            EncryptedDataEncryptionKey = result.EncryptedDataEncryptionKey,
            DataEncryptionKeyId = result.DataEncryptionKeyId,
            DataEncryptionKeyVersion = result.DataEncryptionKeyVersion,
            KeyEncryptionKeySalt = result.KeyEncryptionKeySalt,
            DekKeyMaterialBytes = result.DekKeyMaterialBytes,
            Timestamp = result.Timestamp,
            PathPrefix = result.PathPrefix,
            HashAlgorithm = result.HashAlgorithm?.ToString(),
            ContentType = result.ContentType,
            TenantId = result.TenantId,
            Availability = result.Availability.ToString()
        };
}