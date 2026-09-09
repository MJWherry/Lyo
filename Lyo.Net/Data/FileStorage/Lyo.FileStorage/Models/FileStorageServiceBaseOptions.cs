using Lyo.Compression.Models;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Models;

/// <summary>Shared settings used by every file storage service.</summary>
public abstract class FileStorageServiceBaseOptions
{
    /// <summary>How deep periodic health probes go. Starts as full round-trip I/O.</summary>
    public FileStorageHealthCheckMode HealthCheckMode { get; set; } = FileStorageHealthCheckMode.Full;

    /// <summary>
    /// Write operation counters and histograms through the configured <see cref="Lyo.Metrics.IMetrics" />. Starts as <see langword="true" />. If no metrics service is
    /// registered, the implementation uses a no-op sink.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Hash used for integrity checks and duplicate detection. Starts as Sha256.</summary>
    public HashAlgorithm HashAlgorithm { get; set; } = HashAlgorithm.Sha256;

    /// <summary>If true, hashes are checked before save. A matching hash returns the existing file id instead of writing a duplicate.</summary>
    public bool EnableDuplicateDetection { get; set; } = false;

    /// <summary>What to do with a duplicate when duplicate detection is on.</summary>
    public DuplicateHandlingStrategy DuplicateStrategy { get; set; } = DuplicateHandlingStrategy.ReturnExisting;

    /// <summary>If true, getting a missing file throws FileNotFoundException. If false, returns null or an empty array. Starts as true.</summary>
    public bool ThrowOnFileNotFound { get; set; } = true;

    /// <summary>If true, deleting a missing file throws FileNotFoundException. If false, returns false. Starts as true.</summary>
    public bool ThrowOnDeleteNotFound { get; set; } = true;

    /// <summary>
    /// Max decompressed size in bytes. Stops decompression once this many output bytes are produced, to block decompression bombs. Starts at 10 GiB. Set to
    /// <see langword="null" /> to skip this guard and rely only on <c>CompressionService</c> internal validation.
    /// </summary>
    public long? MaxDecompressedFileSize { get; set; } = 10L * 1024 * 1024 * 1024;

    /// <summary>If true, a hash mismatch throws InvalidDataException (corruption). If false, a warning is logged. Starts as false.</summary>
    public bool ThrowOnHashMismatch { get; set; } = false;

    /// <summary>If true, audit sink failures throw instead of being logged. Starts as false.</summary>
    public bool ThrowOnAuditFailure { get; set; }

    /// <summary>Max upload size in bytes (plaintext / declared size). Null means no limit.</summary>
    public long? MaxUploadSizeBytes { get; set; }

    /// <summary>Allowed Content-Type values, compared case-insensitively. Null or empty means allow all.</summary>
    public HashSet<string>? AllowedContentTypes { get; set; }

    /// <summary>Availability used for new files when the caller does not pass an override. Starts as Available.</summary>
    public FileAvailability DefaultAvailability { get; set; } = FileAvailability.Available;

    /// <summary>If true, Get and presigned read can open Quarantined files (admin tooling).</summary>
    public bool AllowReadQuarantinedForAdmin { get; set; }

    /// <summary>If set, every decompress uses this algorithm instead of per-file metadata. For migration and recovery tooling.</summary>
    public CompressionAlgorithm? DecompressionAlgorithmOverride { get; set; }
}