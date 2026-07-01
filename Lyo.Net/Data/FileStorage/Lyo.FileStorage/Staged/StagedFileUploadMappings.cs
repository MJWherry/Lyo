using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Staged;

/// <summary>Maps persistence rows to API/event DTOs.</summary>
public static class StagedFileUploadMappings
{
    /// <summary>Projects a store row to the public <see cref="StagedFileResult" /> snapshot.</summary>
    public static StagedFileResult ToResult(StagedFileUploadRecord record)
        => new(
            record.StageId, record.Status, record.OriginalFileName, record.ObservedSizeBytes, record.ContentHash, record.ContentType, record.TenantId, record.PathPrefix,
            record.StorageLocation, record.CreatedUtc, record.ExpiresUtc, record.CommittedFileId, record.ProviderKind, record.HashAlgorithm, record.FailureReason);
}