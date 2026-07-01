using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;

namespace Lyo.FileStorage.Models;

/// <summary>Public projection of a staged upload for APIs, events, and workbench responses.</summary>
public sealed record StagedFileResult(
    Guid StageId,
    StagedUploadStatus Status,
    string? OriginalFileName,
    long? ObservedSizeBytes,
    byte[]? ContentHash,
    string? ContentType,
    string? TenantId,
    string? PathPrefix,
    string StorageLocation,
    DateTime CreatedUtc,
    DateTime ExpiresUtc,
    Guid? CommittedFileId,
    MultipartUploadProviderKind ProviderKind,
    HashAlgorithm? HashAlgorithm = null,
    string? FailureReason = null);