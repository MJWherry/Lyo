using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Staged;

namespace Lyo.FileMetadataStore.Postgres.Database;

internal static class StagedFileUploadEntityMapping
{
    internal static StagedFileUploadEntity ToEntity(StagedFileUploadRecord r)
        => new() {
            StageId = r.StageId,
            TenantId = r.TenantId,
            OwnerId = r.OwnerId,
            CreatedUtc = r.CreatedUtc,
            ExpiresUtc = r.ExpiresUtc,
            Status = r.Status,
            StorageLocation = r.StorageLocation,
            PathPrefix = r.PathPrefix,
            OriginalFileName = r.OriginalFileName,
            ContentType = r.ContentType,
            DeclaredMaxSizeBytes = r.DeclaredMaxSizeBytes,
            ObservedSizeBytes = r.ObservedSizeBytes,
            ContentHash = r.ContentHash,
            HashAlgorithm = r.HashAlgorithm?.ToString(),
            ProviderKind = r.ProviderKind,
            ProviderState = r.ProviderStateJson,
            CommittedFileId = r.CommittedFileId,
            FailureReason = r.FailureReason
        };

    internal static StagedFileUploadRecord FromEntity(StagedFileUploadEntity e)
    {
        HashAlgorithm? hashAlgorithm = null;
        if (!string.IsNullOrEmpty(e.HashAlgorithm) && Enum.TryParse<HashAlgorithm>(e.HashAlgorithm, out var hashAlg))
            hashAlgorithm = hashAlg;

        return new(
            e.StageId, e.TenantId, e.OwnerId, e.CreatedUtc, e.ExpiresUtc, e.Status, e.StorageLocation, e.PathPrefix, e.OriginalFileName, e.ContentType, e.DeclaredMaxSizeBytes,
            e.ObservedSizeBytes, e.ContentHash, hashAlgorithm, e.ProviderKind, e.ProviderState, e.CommittedFileId, e.FailureReason);
    }
}