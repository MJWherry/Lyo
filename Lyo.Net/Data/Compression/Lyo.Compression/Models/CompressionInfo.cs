using System.Diagnostics;

namespace Lyo.Compression.Models;

/// <summary>Timing and size statistics for an in-memory compress operation.</summary>
/// <param name="UncompressedSize">Original payload length in bytes.</param>
/// <param name="CompressedSize">Compressed payload length in bytes.</param>
/// <param name="TimeMs">Wall-clock milliseconds spent compressing.</param>
[DebuggerDisplay("{ToString()}")]
public record CompressionInfo(long UncompressedSize, long CompressedSize, long TimeMs)
{
    public double CompressionRatio => UncompressedSize > 0 ? CompressedSize / (double)UncompressedSize : 0;

    public double SpaceSavedPercent => (1 - CompressionRatio) * 100;

    public override string ToString() => $"Uncompressed={UncompressedSize} Compressed={CompressedSize} Ratio={CompressionRatio:P2} Saved={SpaceSavedPercent:P2}% Time={TimeMs}ms";
}