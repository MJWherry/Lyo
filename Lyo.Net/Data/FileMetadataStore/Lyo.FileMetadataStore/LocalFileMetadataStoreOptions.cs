namespace Lyo.FileMetadataStore;

/// <summary>Configuration options for local file metadata store service.</summary>
public sealed class LocalFileMetadataStoreOptions
{
    public const string SectionName = "LocalFileMetadataStore";

    /// <summary>Gets or sets the root directory path for storing metadata files.</summary>
    public string RootDirectoryPath { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to automatically create the root directory if it doesn't exist. Default is true.</summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;
}