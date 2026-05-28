using Lyo.Compression.Models;

namespace Lyo.Compression.BZip2;

/// <summary>BZip2 (Burrows–Wheeler); slower than gzip, often better ratio; typical <c>.bz2</c> streams.</summary>
public sealed record BZip2CompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Canonical singleton.</summary>
    public static readonly BZip2CompressionAlgorithm Instance = new();

    private BZip2CompressionAlgorithm() : base("BZip2", ".bz2") { }
}
