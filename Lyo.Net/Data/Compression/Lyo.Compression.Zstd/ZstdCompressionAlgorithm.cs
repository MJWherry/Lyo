using Lyo.Compression.Models;

namespace Lyo.Compression.Zstd;

/// <summary>Zstandard (Zstd); modern ratio/speed tradeoff via ZstdSharp. Name kept as <c>"ZstdSharp"</c> for wire-format compatibility with the legacy enum value.</summary>
public sealed record ZstdCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Canonical singleton.</summary>
    public static readonly ZstdCompressionAlgorithm Instance = new();

    private ZstdCompressionAlgorithm() : base("ZstdSharp", ".zst") { }
}
