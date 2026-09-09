namespace Lyo.FileMetadataStore;

/// <summary>Options for the local-disk file metadata store.</summary>
public sealed class LocalFileMetadataStoreOptions
{
    public const string SectionName = "LocalFileMetadataStore";

    /// <summary>Root directory for metadata files.</summary>
    public string RootDirectoryPath { get; set; } = string.Empty;

    /// <summary>When true, the root directory is created if it is missing. Starts as true.</summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;
}