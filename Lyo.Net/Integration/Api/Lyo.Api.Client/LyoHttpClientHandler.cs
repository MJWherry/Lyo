using System.Net;
using Lyo.Exceptions;

namespace Lyo.Api.Client;

/// <summary>
/// Primary <see cref="HttpClientHandler" /> that applies <see cref="ApiClientOptions.AcceptEncodings" /> to
/// <see cref="HttpClientHandler.AutomaticDecompression" />. Use with <c>IHttpClientFactory</c> via
/// <see cref="ServiceCollectionExtensions.UseLyoHttpClientHandler{TOptions}" />. Create a new instance per handler lifetime; do not register as a singleton.
/// Replacing the primary handler (proxy, client certificates, test stubs) drops decompression unless the replacement subclasses this type or sets
/// <see cref="HttpClientHandler.AutomaticDecompression" /> itself. Do not also wrap a decompressing <see cref="DelegatingHandler" /> — the body would be inflated twice.
/// </summary>
public class LyoHttpClientHandler : HttpClientHandler
{
    /// <summary>Creates a handler from <paramref name="options" />. When <see cref="ApiClientOptions.EnableAutoResponseDecompression" /> is false, decompression stays off.</summary>
    public LyoHttpClientHandler(ApiClientOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        if (options.EnableAutoResponseDecompression)
            AutomaticDecompression = ToDecompressionMethods(options.AcceptEncodings);
    }

    /// <summary>Maps encoding names (<c>gzip</c>, <c>deflate</c>, <c>br</c>) to <see cref="DecompressionMethods" /> flags. Unknown values are ignored.</summary>
    public static DecompressionMethods ToDecompressionMethods(IEnumerable<string>? encodings)
    {
        var methods = DecompressionMethods.None;
        if (encodings == null)
            return methods;

        foreach (var raw in encodings) {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var encoding = raw.Trim().ToLowerInvariant();
            if (encoding == "gzip")
                methods |= DecompressionMethods.GZip;
            else if (encoding == "deflate")
                methods |= DecompressionMethods.Deflate;
#if !NETSTANDARD2_0
            else if (encoding == "br")
                methods |= DecompressionMethods.Brotli;
#endif
        }

        return methods;
    }

    internal static bool IsSupportedResponseEncoding(string encoding)
    {
        if (encoding is "gzip" or "deflate")
            return true;
#if !NETSTANDARD2_0
        if (encoding == "br")
            return true;
#endif
        return false;
    }
}
