using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Azure;

public sealed class AzureFileStorageOptions : FileStorageServiceBaseOptions
{
    public const string SectionName = "AzureFileStorageOptions";

    /// <summary>The connection string for the Azure Storage account.</summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>The name of the blob container where files will be stored.</summary>
    public string ContainerName { get; set; } = null!;

    /// <summary>Optional prefix to use for all blob names. Useful for organizing files in a container.</summary>
    public string? BlobPrefix { get; set; }

    /// <summary>Enable metrics collection for file storage operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;
}