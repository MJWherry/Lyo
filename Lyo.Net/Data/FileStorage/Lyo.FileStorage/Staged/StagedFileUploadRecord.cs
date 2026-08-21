using System.Diagnostics;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Multipart;

namespace Lyo.FileStorage.Staged;

/// <summary>Domain row for <see cref="IStagedFileUploadStore" /> before canonical <c>file_metadata</c> exists.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StagedFileUploadRecord(
    Guid StageId,
    string? TenantId,
    Guid? OwnerId,
    DateTime CreatedUtc,
    DateTime ExpiresUtc,
    StagedUploadStatus Status,
    string StorageLocation,
    string? PathPrefix,
    string? OriginalFileName,
    string? ContentType,
    long DeclaredMaxSizeBytes,
    long? ObservedSizeBytes,
    byte[]? ContentHash,
    HashAlgorithm? HashAlgorithm,
    MultipartUploadProviderKind ProviderKind,
    string ProviderStateJson,
    Guid? CommittedFileId,
    string? FailureReason)
{
    /// <inheritdoc />
    public override string ToString()
        => $"StagedFileUploadRecord: StageId={StageId}, Status={Status}, OriginalFileName={OriginalFileName ?? "(none)"}, CommittedFileId={CommittedFileId?.ToString() ?? "(none)"}";
}