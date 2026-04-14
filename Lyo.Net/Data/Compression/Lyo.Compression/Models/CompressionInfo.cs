using System.Diagnostics;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString()}")]
public record CompressionInfo(long UncompressedSize, long CompressedSize, long TimeMs)
{
    public double CompressionRatio => UncompressedSize > 0 ? CompressedSize / (double)UncompressedSize : 0;

    public double SpaceSavedPercent => (1 - CompressionRatio) * 100;

    public override string ToString() => $"Uncompressed={UncompressedSize} Compressed={CompressedSize} Ratio={CompressionRatio:P2} Saved={SpaceSavedPercent:P2}% Time={TimeMs}ms";
}