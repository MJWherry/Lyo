using System.Diagnostics;

namespace Lyo.Api.Models.File;

[DebuggerDisplay("{ToString(),nq}")]
public record FileUpload(
    string OriginalFileName,
    long OriginalFileSize,
    byte[] OriginalFileHash,
    string SourceFileName,
    long SourceFileSize,
    byte[] SourceFileHash,
    bool IsCompressed,
    long? CompressedFileSize,
    byte[]? CompressedFileHash,
    bool IsEncrypted,
    long? EncryptedFileSize,
    byte[]? EncryptedFileHash,
    byte[]? EncryptedDataEncryptionKey,
    int? DataEncryptionKeyVersion,
    DateTime UploadTimestamp)
{
    /// <summary>File name, size, and hash lengths. Hash and key bytes are omitted.</summary>
    public override string ToString()
        => $"{OriginalFileName} {OriginalFileSize}B Zip={IsCompressed} Enc={IsEncrypted} Hash={OriginalFileHash.Length} Dek={EncryptedDataEncryptionKey?.Length ?? 0}";
}