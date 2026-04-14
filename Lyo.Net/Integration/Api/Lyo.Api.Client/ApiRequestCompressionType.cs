namespace Lyo.Api.Client;

/// <summary>Compression algorithm to use for JSON request bodies.</summary>
public enum ApiRequestCompressionType
{
    None = 0,
    Gzip = 1,
    Deflate = 2,
    Brotli = 3
}