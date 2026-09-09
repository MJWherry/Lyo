using System.Diagnostics;

namespace Lyo.Compression.Models;

/// <summary>Timing and size stats for an in-memory decompress.</summary>
/// <param name="CompressedSize">Input compressed length in bytes.</param>
/// <param name="DecompressedSize">Output restored length in bytes.</param>
/// <param name="DecompressionTimeMs">Wall-clock milliseconds spent decompressing.</param>
[DebuggerDisplay("{ToString(),nq}")]
public record DecompressionInfo(long CompressedSize, long DecompressedSize, long DecompressionTimeMs)
{
    /// <summary>DecompressedSize / CompressedSize. Values above 1 mean expansion.</summary>
    public double ExpansionRatio => CompressedSize > 0 ? DecompressedSize / (double)CompressedSize : 0;

    /// <summary>Percent size increase from compressed to decompressed. Positive values mean expansion.</summary>
    public double SizeIncreasePercent => (ExpansionRatio - 1) * 100;

    public override string ToString()
        => $"Compressed={CompressedSize} Decompressed={DecompressedSize} Ratio={ExpansionRatio:P2} Increased={SizeIncreasePercent:P2}% Time={DecompressionTimeMs}ms";
}