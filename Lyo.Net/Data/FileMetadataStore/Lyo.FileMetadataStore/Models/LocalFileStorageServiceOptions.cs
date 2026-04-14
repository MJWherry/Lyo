using System.Text.Json.Serialization;

namespace Lyo.FileMetadataStore.Models;

public sealed class LocalFileStorageServiceOptions
{
    public const string SectionName = "LocalFileStorageService";

    public string RootDirectoryPath { get; init; } = null!;

    /// <summary>Path to SQLite database file. If null, uses JSON file-based metadata storage. If specified, metadata will be stored in SQLite for better performance and querying.</summary>
    public string? DatabasePath { get; init; }

    /// <summary>Enable duplicate detection by checking file hashes before saving. When enabled, files with the same hash will return the existing file ID instead of creating a duplicate.</summary>
    public bool EnableDuplicateDetection { get; init; } = false;

    /// <summary>Strategy for handling duplicate files when duplicate detection is enabled.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DuplicateHandlingStrategy DuplicateStrategy { get; init; } = DuplicateHandlingStrategy.ReturnExisting;
}