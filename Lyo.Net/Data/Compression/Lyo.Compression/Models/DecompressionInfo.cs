using System.Diagnostics;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString()}")]
public record DecompressionInfo(long CompressedSize, long DecompressedSize, long DecompressionTimeMs)
{
    /// <summary>DecompressedSize / CompressedSize. Values > 1 indicate expansion.</summary>
    public double ExpansionRatio => CompressedSize > 0 ? DecompressedSize / (double)CompressedSize : 0;

    /// <summary>Percentage increase in size from compressed to decompressed. Positive values indicate expansion.</summary>
    public double SizeIncreasePercent => (ExpansionRatio - 1) * 100;

    public override string ToString()
        => $"Compressed={CompressedSize} Decompressed={DecompressedSize} Ratio={ExpansionRatio:P2} Increased={SizeIncreasePercent:P2}% Time={DecompressionTimeMs}ms";
}