using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Multipart;

namespace Lyo.FileStorage.Staged;

/// <summary>Domain row for <see cref="IStagedFileUploadStore" /> before canonical <c>file_metadata</c> exists.</summary>
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
    string? FailureReason);