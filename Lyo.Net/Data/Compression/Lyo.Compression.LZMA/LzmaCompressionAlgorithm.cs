using Lyo.Compression.Models;

namespace Lyo.Compression.LZMA;

/// <summary>LZMA / LZMA2-style high-compression algorithm; typical <c>.lzma</c> streams.</summary>
public sealed record LzmaCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Canonical singleton.</summary>
    public static readonly LzmaCompressionAlgorithm Instance = new();

    private LzmaCompressionAlgorithm()
        : base("LZMA", ".lzma") { }
}