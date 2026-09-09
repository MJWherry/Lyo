using Lyo.Compression.Models;

namespace Lyo.Compression.BZip2;

/// <summary>BZip2 (Burrows-Wheeler). Slower than gzip, often a better ratio. Typical <c>.bz2</c> streams.</summary>
public sealed record BZip2CompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly BZip2CompressionAlgorithm Instance = new();

    private BZip2CompressionAlgorithm()
        : base("BZip2", ".bz2", null, [[0x42, 0x5A, 0x68]]) { } // "BZh"
}