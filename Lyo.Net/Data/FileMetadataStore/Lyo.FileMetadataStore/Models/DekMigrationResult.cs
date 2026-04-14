namespace Lyo.FileMetadataStore.Models;

/// <summary>Result of a DEK migration operation, containing statistics about the migration process.</summary>
public record DekMigrationResult(int TotalFilesFound, int SuccessfullyMigrated, int Failed, IReadOnlyList<Guid> FailedFileIds, IReadOnlyList<string> Errors)
{
    /// <summary>Indicates whether all files were successfully migrated.</summary>
    public bool AllSucceeded => Failed == 0;
}