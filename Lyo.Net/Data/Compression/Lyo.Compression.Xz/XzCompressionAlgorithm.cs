using Lyo.Compression.Models;

namespace Lyo.Compression.Xz;

/// <summary>XZ container (LZMA2 filter). Strong ratio, common on Unix archives. Typical <c>.xz</c> streams.</summary>
public sealed record XzCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly XzCompressionAlgorithm Instance = new();

    private XzCompressionAlgorithm()
        : base("XZ", ".xz", null, [[0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00]]) { } // .xz container header
}