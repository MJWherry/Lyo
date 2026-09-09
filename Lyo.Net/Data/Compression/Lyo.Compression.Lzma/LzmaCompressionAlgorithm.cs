using Lyo.Compression.Models;

namespace Lyo.Compression.Lzma;

/// <summary>LZMA / LZMA2-style high-compression algorithm. Typical <c>.lzma</c> streams.</summary>
public sealed record LzmaCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly LzmaCompressionAlgorithm Instance = new();

    private LzmaCompressionAlgorithm()
        : base("LZMA", ".lzma") { }
}