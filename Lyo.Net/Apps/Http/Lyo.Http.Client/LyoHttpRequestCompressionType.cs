namespace Lyo.Http.Client;

/// <summary>Which compression to apply to JSON request bodies.</summary>
public enum LyoHttpRequestCompressionType
{
    /// <summary>Send JSON uncompressed.</summary>
    None = 0,

    /// <summary>GZIP (RFC 1952).</summary>
    Gzip = 1,

    /// <summary>Raw DEFLATE (RFC 1951).</summary>
    Deflate = 2,

    /// <summary>Brotli (RFC 7932). Unsupported on netstandard2.0 request bodies.</summary>
    Brotli = 3
}
