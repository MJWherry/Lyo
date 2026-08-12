using Lyo.Compression.Models;

namespace Lyo.Compression.Lz4;

/// <summary>
/// LZ4 block compression; very fast, moderate compression. Registers itself with <see cref="CompressionAlgorithm.TryFromExtension" /> /
/// <see cref="CompressionAlgorithm.TryFromName" /> as soon as <see cref="Instance" /> is touched (typically via <c>services.AddLz4Compressor()</c>).
/// </summary>
public sealed record Lz4CompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Canonical singleton; use this instance everywhere instead of <c>new Lz4CompressionAlgorithm()</c>.</summary>
    public static readonly Lz4CompressionAlgorithm Instance = new();

    private Lz4CompressionAlgorithm()
        : base("LZ4", ".lz4") { }
}