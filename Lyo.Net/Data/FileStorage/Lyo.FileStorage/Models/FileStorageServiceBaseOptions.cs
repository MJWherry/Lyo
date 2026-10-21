using System.Text.RegularExpressions;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Records;
using Lyo.Compression.Models;
using Lyo.Exceptions;
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

    /// <summary>
    /// Whitelist of Content-Type values, compared case-insensitively. Null means this side is not configured. An empty set denies every content type. Cannot be set together with
    /// <see cref="DeniedContentTypes" />.
    /// </summary>
    public HashSet<string>? AllowedContentTypes { get; set; }

    /// <summary>
    /// Blacklist of Content-Type values, compared case-insensitively. Null means this side is not configured. An empty set denies nothing. Cannot be set together with
    /// <see cref="AllowedContentTypes" />.
    /// </summary>
    public HashSet<string>? DeniedContentTypes { get; set; }

    /// <summary>
    /// Whitelist of regular expressions matched against the full original file name (<see cref="Regex.IsMatch(string, string)" />). Patterns are not anchored. Null means this
    /// side is not configured. An empty list rejects every name. Cannot be set together with <see cref="DeniedFileNamePatterns" />.
    /// </summary>
    public List<string>? AllowedFileNamePatterns { get; set; }

    /// <summary>
    /// Blacklist of regular expressions matched against the full original file name. Patterns are not anchored. Null means this side is not configured. An empty list rejects
    /// nothing. Cannot be set together with <see cref="AllowedFileNamePatterns" />.
    /// </summary>
    public List<string>? DeniedFileNamePatterns { get; set; }

    /// <summary>
    /// Whitelist of sniffed <see cref="FileTypeInfo.Name" /> values, compared case-insensitively. Null means this side is not configured. An empty set rejects every header,
    /// including unrecognized bytes. A name that shares a signature family with the sniffed type also matches, so <c>JAR</c> selects the same <c>PK</c> bucket as <c>ZIP</c>. Cannot be
    /// set together with <see cref="DeniedSniffedFileTypes" />.
    /// </summary>
    public HashSet<string>? AllowedSniffedFileTypes { get; set; }

    /// <summary>
    /// Blacklist of sniffed <see cref="FileTypeInfo.Name" /> values, compared case-insensitively. Null means this side is not configured. An empty set rejects nothing.
    /// Unrecognized headers pass. A name that shares a signature family with the sniffed type also matches. Cannot be set together with <see cref="AllowedSniffedFileTypes" />.
    /// </summary>
    public HashSet<string>? DeniedSniffedFileTypes { get; set; }

    /// <summary>When true, the file header must agree with the claimed file type. Starts as false.</summary>
    public bool VerifyFileSignature { get; set; }

    /// <summary>Availability used for new files when the caller does not pass an override. Starts as Available.</summary>
    public FileAvailability DefaultAvailability { get; set; } = FileAvailability.Available;

    /// <summary>If true, Get and presigned read can open Quarantined files (admin tooling).</summary>
    public bool AllowReadQuarantinedForAdmin { get; set; }

    /// <summary>If set, every decompress uses this algorithm instead of per-file metadata. For migration and recovery tooling.</summary>
    public CompressionAlgorithm? DecompressionAlgorithmOverride { get; set; }

    /// <summary>
    /// How long a direct upload may stay pending after begin before <see cref="Abstractions.IFileStorageService.SweepExpiredDirectUploadsAsync" /> discards it and any object
    /// behind it. Begin rejects a <see cref="DirectUploadBeginRequest.UrlExpiration" /> longer than this, so a URL never outlives its row. Starts at 7 days, the longest presigned URL S3
    /// and Azure issue.
    /// </summary>
    public TimeSpan PendingDirectUploadTtl { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Throws when both sides of a content-type, file-name, or sniffed-type pair are set, when a file-name pattern does not compile, or when a sniffed-type name is not in
    /// <see cref="FileTypeInfo.All" />.
    /// </summary>
    /// <exception cref="ArgumentException">A filter pair is contradictory, a pattern is invalid, or a sniffed-type name is unknown.</exception>
    public void ValidateUploadFilters()
    {
        ThrowIfBoth(AllowedContentTypes, DeniedContentTypes, nameof(AllowedContentTypes), nameof(DeniedContentTypes));
        ThrowIfBoth(AllowedFileNamePatterns, DeniedFileNamePatterns, nameof(AllowedFileNamePatterns), nameof(DeniedFileNamePatterns));
        ThrowIfBoth(AllowedSniffedFileTypes, DeniedSniffedFileTypes, nameof(AllowedSniffedFileTypes), nameof(DeniedSniffedFileTypes));
        ThrowIfPatternInvalid(AllowedFileNamePatterns, nameof(AllowedFileNamePatterns));
        ThrowIfPatternInvalid(DeniedFileNamePatterns, nameof(DeniedFileNamePatterns));
        ThrowIfSniffedNamesUnknown(AllowedSniffedFileTypes, nameof(AllowedSniffedFileTypes));
        ThrowIfSniffedNamesUnknown(DeniedSniffedFileTypes, nameof(DeniedSniffedFileTypes));
    }

    private static void ThrowIfBoth(object? allowed, object? denied, string allowedName, string deniedName)
        => ArgumentHelpers.ThrowIf(allowed is not null && denied is not null, $"{allowedName} and {deniedName} cannot both be set.");

    private static void ThrowIfPatternInvalid(List<string>? patterns, string paramName)
    {
        if (patterns is null)
            return;

        foreach (var pattern in patterns) {
            if (pattern.IsNullOrWhitespace())
                throw new ArgumentException("File name pattern cannot be empty.", paramName);

            try {
                _ = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            }
            catch (ArgumentException ex) {
                throw new ArgumentException($"File name pattern '{pattern}' is not a valid regular expression.", paramName, ex);
            }
        }
    }

    private static void ThrowIfSniffedNamesUnknown(HashSet<string>? names, string paramName)
    {
        if (names is null)
            return;

        foreach (var name in names) {
            if (name.IsNullOrWhitespace() || !FileTypeInfo.All.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"'{name}' is not a FileTypeInfo name.", paramName);
        }
    }
}