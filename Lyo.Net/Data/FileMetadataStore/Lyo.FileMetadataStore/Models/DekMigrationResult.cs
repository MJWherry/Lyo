using System.Diagnostics;

namespace Lyo.FileMetadataStore.Models;

/// <summary>Outcome of a DEK migration, including counts and failed file IDs.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record DekMigrationResult(int TotalFilesFound, int SuccessfullyMigrated, int Failed, IReadOnlyList<Guid> FailedFileIds, IReadOnlyList<string> Errors, int Skipped = 0)
{
    /// <summary>True when no files failed.</summary>
    public bool AllSucceeded => Failed == 0;

    public override string ToString() => $"DekMigrationResult: found={TotalFilesFound}, migrated={SuccessfullyMigrated}, failed={Failed}, skipped={Skipped}";
}