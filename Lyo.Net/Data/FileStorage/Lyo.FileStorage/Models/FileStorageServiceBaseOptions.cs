using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Models;

/// <summary>Base options class for file storage services containing common configuration options.</summary>
public abstract class FileStorageServiceBaseOptions
{
    /// <summary>Hash algorithm used for file integrity verification and duplicate detection. Default: Sha256.</summary>
    public HashAlgorithm HashAlgorithm { get; set; } = HashAlgorithm.Sha256;

    /// <summary>Enable duplicate detection by checking file hashes before saving. When enabled, files with the same hash will return the existing file ID instead of creating a duplicate.</summary>
    public bool EnableDuplicateDetection { get; set; } = false;

    /// <summary>Strategy for handling duplicate files when duplicate detection is enabled.</summary>
    public DuplicateHandlingStrategy DuplicateStrategy { get; set; } = DuplicateHandlingStrategy.ReturnExisting;

    /// <summary>If true, throws FileNotFoundException when getting a file that doesn't exist. If false, returns null or empty array. Default: true</summary>
    public bool ThrowOnFileNotFound { get; set; } = true;

    /// <summary>If true, throws FileNotFoundException when deleting a file that doesn't exist. If false, returns false. Default: true</summary>
    public bool ThrowOnDeleteNotFound { get; set; } = true;

    /// <summary>Maximum allowed decompressed file size in bytes. Prevents decompression bombs. Null means no limit (relies on CompressionService validation). Default: 10 GB.</summary>
    public long? MaxDecompressedFileSize { get; set; } = 10L * 1024 * 1024 * 1024;

    /// <summary>If true, throws InvalidDataException when computed hash does not match stored hash (indicating corruption). If false, logs a warning. Default: false</summary>
    public bool ThrowOnHashMismatch { get; set; } = false;

    /// <summary>If true, audit sink failures throw instead of being logged. Default: false</summary>
    public bool ThrowOnAuditFailure { get; set; }

    /// <summary>Maximum upload size in bytes (plaintext / declared size). Null means no limit.</summary>
    public long? MaxUploadSizeBytes { get; set; }

    /// <summary>Allowed Content-Type values (case-insensitive). Null or empty means allow all.</summary>
    public HashSet<string>? AllowedContentTypes { get; set; }

    /// <summary>When true, new files start as PendingScan until malware scan passes.</summary>
    public bool RequireScanBeforeAvailable { get; set; }

    /// <summary>Availability assigned when RequireScanBeforeAvailable is false. Default: Available.</summary>
    public FileAvailability DefaultAvailability { get; set; } = FileAvailability.Available;

    /// <summary>When true, Get and presigned read can access Quarantined files (e.g. admin tooling).</summary>
    public bool AllowReadQuarantinedForAdmin { get; set; }
}