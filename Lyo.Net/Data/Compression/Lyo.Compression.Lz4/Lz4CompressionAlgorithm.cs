using Lyo.Compression.Models;

namespace Lyo.Compression.Lz4;

/// <summary>
/// LZ4 block compression. Very fast, moderate ratio. Registers with <see cref="CompressionAlgorithm.TryFromExtension" /> and
/// <see cref="CompressionAlgorithm.TryFromName" /> the first time <see cref="Instance" /> is touched, usually via <c>services.AddLz4Compressor()</c>.
/// </summary>
public sealed record Lz4CompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Shared singleton. Use this instance instead of <c>new Lz4CompressionAlgorithm()</c>.</summary>
    public static readonly Lz4CompressionAlgorithm Instance = new();

    private Lz4CompressionAlgorithm()
        : base("LZ4", ".lz4", null, [[0x04, 0x22, 0x4D, 0x18]]) { } // LZ4 frame magic
}