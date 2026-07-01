namespace Lyo.FileStorage.Staged;

/// <summary>Lifecycle of a row in <see cref="IStagedFileUploadStore" /> / <c>staged_file_upload</c>. Persisted as varchar enum names in Postgres/Sqlite.</summary>
public enum StagedUploadStatus
{
    /// <summary>Presigned PUT issued; awaiting client upload.</summary>
    PendingUpload = 0,

    /// <summary>Object verified and hashed; ready for <see cref="IStagedFileUploadService.CommitAsync" />.</summary>
    Uploaded = 1,

    /// <summary>Commit in progress (<see cref="IStagedFileUploadStore.TryTransitionStatusAsync" /> claim).</summary>
    Committing = 2,

    /// <summary>Canonical file metadata written; <see cref="StagedFileUploadRecord.CommittedFileId" /> is set.</summary>
    Committed = 3,

    /// <summary>User or operator aborted; staging object deleted best-effort.</summary>
    Aborted = 4,

    /// <summary>Session TTL elapsed (reserved for expiry jobs).</summary>
    Expired = 5,

    /// <summary>Complete or commit failed; see <see cref="StagedFileUploadRecord.FailureReason" />.</summary>
    Failed = 6
}