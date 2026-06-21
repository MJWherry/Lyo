namespace Lyo.FileStorage.Staged;

/// <summary>
/// OLTP persistence for in-flight staged uploads. Postgres/Sqlite implementations map to the <c>staged_file_upload</c> table; use
/// <see cref="InMemoryStagedFileUploadStore" /> for tests and single-node dev.
/// </summary>
public interface IStagedFileUploadStore
{
    /// <summary>Inserts a new stage row. Implementations should reject duplicate <see cref="StagedFileUploadRecord.StageId" /> values.</summary>
    Task CreateAsync(StagedFileUploadRecord record, CancellationToken ct = default);

    /// <summary>Returns the row when present; otherwise <see langword="null" />.</summary>
    Task<StagedFileUploadRecord?> GetAsync(Guid stageId, CancellationToken ct = default);

    /// <summary>Overwrites an existing row; throws when the stage id is unknown.</summary>
    Task UpdateAsync(StagedFileUploadRecord record, CancellationToken ct = default);

    /// <summary>Optimistic claim for worker commit. Returns <see langword="false" /> when current status does not match <paramref name="from" />.</summary>
    Task<bool> TryTransitionStatusAsync(Guid stageId, StagedUploadStatus from, StagedUploadStatus to, CancellationToken ct = default);
}
