namespace Lyo.Api.Models.File;

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
    DateTime UploadTimestamp);