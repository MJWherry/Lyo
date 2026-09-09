using Lyo.Http.Client;

namespace Lyo.Api.Client;

/// <summary>Which compression to apply to JSON request bodies. Prefer <see cref="LyoHttpRequestCompressionType" />.</summary>
[Obsolete("Use Lyo.Http.Client.LyoHttpRequestCompressionType.")]
public enum ApiRequestCompressionType
{
    None = 0,
    Gzip = 1,
    Deflate = 2,
    Brotli = 3
}
