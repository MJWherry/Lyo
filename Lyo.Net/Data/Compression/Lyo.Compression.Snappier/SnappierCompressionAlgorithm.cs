using Lyo.Compression.Models;

namespace Lyo.Compression.Snappier;

/// <summary>Snappy framing (Snappier). Very fast, less CPU than zlib on warm payloads. Default extension <c>.snappy</c>.</summary>
public sealed record SnappierCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly SnappierCompressionAlgorithm Instance = new();

    /// <summary>
    /// Snappy's raw block format (EasyCompressor's binary API) cannot be read by its framed stream format, and EasyCompressor has no stream-compatible binary mode, so
    /// byte[] compression has to go through the stream path.
    /// </summary>
    public override bool BinaryCompressMatchesStreamFormat => false;

    private SnappierCompressionAlgorithm()
        // Framed format only: stream identifier chunk (0xFF, length 6, "sNaPpY"). Raw Snappy blocks have no header.
        : base("Snappier", ".snappy", null, [[0xFF, 0x06, 0x00, 0x00, 0x73, 0x4E, 0x61, 0x50, 0x70, 0x59]]) { }
}