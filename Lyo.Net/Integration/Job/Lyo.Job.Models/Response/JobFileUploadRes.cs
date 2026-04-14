using System.Diagnostics;
using Lyo.Api.Models.Common.Response;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobFileUploadRes(
    Guid Id,
    Guid JobRunId,
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
    : FileUploadRes(
        Id, OriginalFileName, OriginalFileSize, OriginalFileHash, SourceFileName, SourceFileSize, SourceFileHash, IsCompressed, CompressedFileSize, CompressedFileHash, IsEncrypted,
        EncryptedFileSize, EncryptedFileHash, EncryptedDataEncryptionKey, DataEncryptionKeyVersion, UploadTimestamp);