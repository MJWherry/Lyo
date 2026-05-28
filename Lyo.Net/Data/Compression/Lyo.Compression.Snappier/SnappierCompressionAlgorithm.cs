using Lyo.Compression.Models;

namespace Lyo.Compression.Snappier;

/// <summary>Snappy framing (Snappier); very fast, lower CPU than zlib for warm payloads. Default extension <c>.snappy</c>.</summary>
public sealed record SnappierCompressionAlgorithm : CompressionAlgorithm
{
    /// <summary>Canonical singleton.</summary>
    public static readonly SnappierCompressionAlgorithm Instance = new();

    private SnappierCompressionAlgorithm()
        : base("Snappier", ".snappy") { }
}