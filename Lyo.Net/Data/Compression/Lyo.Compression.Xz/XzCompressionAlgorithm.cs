using Lyo.Compression.Models;

namespace Lyo.Compression.Xz;

/// <summary>XZ container (LZMA2 filter); strong ratio, common on Unix archives. Typical <c>.xz</c> streams.</summary>
public sealed record XzCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Canonical singleton.</summary>
    public static readonly XzCompressionAlgorithm Instance = new();

    private XzCompressionAlgorithm()
        : base("XZ", ".xz") { }
}