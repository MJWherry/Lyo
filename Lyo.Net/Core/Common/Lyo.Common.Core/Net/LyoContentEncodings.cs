namespace Lyo.Common.Core.Net;

/// <summary>
/// HTTP <c>Content-Encoding</c> / <c>Accept-Encoding</c> tokens (RFC 9110 §8.4.1). These are wire values, so they stay lowercase and are never localized; use them instead of
/// re-typing the literals wherever a request or response encoding is read or advertised.
/// </summary>
/// <remarks><c>Lyo.Compression</c>'s <c>CompressionAlgorithm.ContentEncoding</c> resolves to these same tokens, so the two catalogs cannot drift.</remarks>
public static class LyoContentEncodings
{
    /// <summary>GZIP encoding (RFC 1952).</summary>
    public const string GZip = "gzip";

    /// <summary>Raw DEFLATE (RFC 1951). HTTP's <c>deflate</c> token is historically ambiguous between raw DEFLATE and ZLIB-wrapped DEFLATE.</summary>
    public const string Deflate = "deflate";

    /// <summary>Brotli encoding (RFC 7932).</summary>
    public const string Brotli = "br";

    /// <summary>Zstandard encoding (RFC 8878).</summary>
    public const string Zstd = "zstd";

    /// <summary>Identity encoding: no transformation.</summary>
    public const string Identity = "identity";
}
