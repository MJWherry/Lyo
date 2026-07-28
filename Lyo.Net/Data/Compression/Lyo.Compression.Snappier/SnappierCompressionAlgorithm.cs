using Lyo.Compression.Models;

namespace Lyo.Compression.Snappier;

/// <summary>Snappy framing (Snappier); very fast, lower CPU than zlib for warm payloads. Default extension <c>.snappy</c>.</summary>
public sealed record SnappierCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Canonical singleton.</summary>
    public static readonly SnappierCompressionAlgorithm Instance = new();

    /// <summary>
    /// Snappy's raw block format (EasyCompressor's binary API) is not readable by its framed stream format, and EasyCompressor offers no stream-compatible binary mode — so
    /// byte[] compression must go through the stream path.
    /// </summary>
    public override bool BinaryCompressMatchesStreamFormat => false;

    private SnappierCompressionAlgorithm()
        : base("Snappier", ".snappy") { }
}