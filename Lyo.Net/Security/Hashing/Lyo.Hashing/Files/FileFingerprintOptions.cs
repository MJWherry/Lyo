namespace Lyo.Hashing.Files;

/// <summary>Thresholds and sample sizes for <see cref="SparseFileFingerprinter" />.</summary>
public sealed class FileFingerprintOptions
{
    public static FileFingerprintOptions Default { get; } = new();

    /// <inheritdoc cref="SparseFileFingerprinter.DefaultLargeFileThreshold" />
    public long LargeFileThreshold { get; set; } = SparseFileFingerprinter.DefaultLargeFileThreshold;

    /// <inheritdoc cref="SparseFileFingerprinter.DefaultVeryLargeThreshold" />
    public long VeryLargeThreshold { get; set; } = SparseFileFingerprinter.DefaultVeryLargeThreshold;

    /// <inheritdoc cref="SparseFileFingerprinter.DefaultSampleSize" />
    public int SampleSize { get; set; } = SparseFileFingerprinter.DefaultSampleSize;

    /// <inheritdoc cref="SparseFileFingerprinter.DefaultVeryLargeSampleSize" />
    public int VeryLargeSampleSize { get; set; } = SparseFileFingerprinter.DefaultVeryLargeSampleSize;
}