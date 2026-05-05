namespace Lyo.Compression.Models;

/// <summary>Progress snapshot for long-running compression work (bytes processed vs total).</summary>
public class CompressionProgress
{
    public long BytesProcessed { get; init; }

    public long TotalBytes { get; init; }

    public double PercentComplete => TotalBytes > 0 ? (double)BytesProcessed / TotalBytes * 100 : 0;

    public long ElapsedMs { get; init; }
}