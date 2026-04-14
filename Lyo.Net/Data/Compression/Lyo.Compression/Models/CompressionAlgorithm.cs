namespace Lyo.Compression.Models;

/// <summary>
/// Identifies which stream compression implementation to use. Maps to default file extensions and compressor bindings in <see cref="Constants.Data.AlgorithmExtensions" /> (see <c>Lyo.Compression.Constants.Data</c>).
/// </summary>
public enum CompressionAlgorithm
{
#if !NETSTANDARD2_0
    /// <summary>Brotli (RFC 7932); strong ratio, common for HTTP and static assets. Not available on <c>netstandard2.0</c>.</summary>
    Brotli,
#endif
    /// <summary>BZip2 (Burrows–Wheeler); slower than gzip, often better ratio; typical <c>.bz2</c> streams.</summary>
    BZip2,
    /// <summary>Raw DEFLATE bitstream (no zlib/gzip wrapper).</summary>
    Deflate,
    /// <summary>GZIP container around DEFLATE; ubiquitous <c>.gz</c> format.</summary>
    GZip,
    /// <summary>LZ4 block compression; very fast, moderate compression.</summary>
    LZ4,
    /// <summary>LZMA / LZMA2-style high-compression algorithm; typical <c>.lzma</c> streams.</summary>
    LZMA,
    /// <summary>Snappy framing (Snappier); very fast, lower CPU than zlib for warm payloads.</summary>
    Snappier,
    /// <summary>XZ container (LZMA2 filter); strong ratio, common on Unix archives. Typical <c>.xz</c> streams.</summary>
    XZ,
#if !NETSTANDARD2_0
    /// <summary>ZLIB (RFC 1950) wrapper around DEFLATE. Not available on <c>netstandard2.0</c>.</summary>
    ZLib,
#endif
    /// <summary>Zstandard (Zstd); modern ratio/speed tradeoff via ZstdSharp.</summary>
    ZstdSharp
}
