using Lyo.Common.Core.Net;
using Lyo.Compression.Models;

namespace Lyo.Compression.Zstd;

/// <summary>Zstandard (Zstd). Modern ratio and speed via ZstdSharp. The name stays <c>"ZstdSharp"</c> so the wire format matches the older enum value.</summary>
public sealed record ZstdCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly ZstdCompressionAlgorithm Instance = new();

    private ZstdCompressionAlgorithm()
        : base("ZstdSharp", ".zst", LyoContentEncodings.Zstd, [[0x28, 0xB5, 0x2F, 0xFD]]) { } // Zstandard frame magic
}