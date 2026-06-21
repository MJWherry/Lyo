using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Staged;

/// <summary>
/// Two-phase upload: clients PUT bytes to a staging key, then the host verifies (<see cref="CompleteAsync" />) and optionally commits into canonical
/// <see cref="IFileStorageService" /> storage (<see cref="CommitAsync" />). Session state lives in <see cref="IStagedFileUploadStore" /> — not
/// <c>file_metadata</c> until commit.
/// </summary>
/// <remarks>
/// Typical flow: <see cref="BeginAsync" /> → client PUT to <see cref="StagedUploadBeginResult.PresignedPutUrl" /> → <see cref="CompleteAsync" /> →
/// <see cref="CommitAsync" /> (compress/encrypt pipeline) or worker handoff via <see cref="UploadCompleted" /> / <see cref="IStagedFileUploadEventHandler" />.
/// </remarks>
public interface IStagedFileUploadService
{
    /// <summary>Raised after a presigned PUT URL is issued and the <see cref="IStagedFileUploadStore" /> row is created.</summary>
    event EventHandler<StagedUploadPresignedCreatedEventArgs>? PresignedCreated;

    /// <summary>Raised after <see cref="CompleteAsync" /> succeeds (object verified and hashed).</summary>
    event EventHandler<StagedUploadCompletedEventArgs>? UploadCompleted;

    /// <summary>Raised when complete or commit fails and the stage transitions to <see cref="StagedUploadStatus.Failed" />.</summary>
    event EventHandler<StagedUploadFailedEventArgs>? UploadFailed;

    /// <summary>Raised after <see cref="CommitAsync" /> persists canonical file metadata.</summary>
    event EventHandler<StagedUploadCommittedEventArgs>? Committed;

    /// <summary>Creates a stage row and returns a presigned/SAS PUT URL targeting <c>…/.stage/{stageId}/object</c>.</summary>
    Task<StagedUploadBeginResult> BeginAsync(StagedUploadBeginRequest request, CancellationToken ct = default);

    /// <summary>Verifies the staged object exists, hashes it, and transitions status to <see cref="StagedUploadStatus.Uploaded" />.</summary>
    Task<StagedFileResult> CompleteAsync(Guid stageId, StagedUploadCompleteRequest? request = null, CancellationToken ct = default);

    /// <summary>Streams the staged object through the normal save pipeline and writes <c>file_metadata</c>.</summary>
    Task<FileStoreResult> CommitAsync(Guid stageId, StagedUploadCommitRequest request, CancellationToken ct = default);

    /// <summary>Best-effort delete of the staging object and transition to <see cref="StagedUploadStatus.Aborted" />.</summary>
    Task AbortAsync(Guid stageId, CancellationToken ct = default);

    /// <summary>Returns the current stage snapshot; throws <see cref="FileNotFoundException" /> when unknown.</summary>
    Task<StagedFileResult> GetAsync(Guid stageId, CancellationToken ct = default);
}
