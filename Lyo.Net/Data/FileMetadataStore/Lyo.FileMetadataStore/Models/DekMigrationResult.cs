using System.Diagnostics;

namespace Lyo.FileMetadataStore.Models;

/// <summary>Result of a DEK migration operation, containing statistics about the migration process.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record DekMigrationResult(int TotalFilesFound, int SuccessfullyMigrated, int Failed, IReadOnlyList<Guid> FailedFileIds, IReadOnlyList<string> Errors, int Skipped = 0)
{
    /// <summary>Indicates whether all files were successfully migrated.</summary>
    public bool AllSucceeded => Failed == 0;

    public override string ToString() => $"DekMigrationResult: found={TotalFilesFound}, migrated={SuccessfullyMigrated}, failed={Failed}, skipped={Skipped}";
}