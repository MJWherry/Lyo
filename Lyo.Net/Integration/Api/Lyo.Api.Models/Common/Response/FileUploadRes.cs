using System.Diagnostics;
using Lyo.Common.Records;

namespace Lyo.Api.Models.Common.Response;

[DebuggerDisplay("{ToString(),nq}")]
public record FileUploadRes(
    Guid Id,
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
    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => $"{Id} {FileSizeUnitInfo.FormatBestFitAbbreviation(OriginalFileSize)}";
}