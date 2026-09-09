using System.Net;
using Lyo.Common.Core.Net;
using Lyo.Exceptions;

namespace Lyo.Http.Client;

/// <summary>
/// Primary <see cref="HttpClientHandler" /> that applies <see cref="LyoHttpClientOptions.AcceptEncodings" /> to
/// <see cref="HttpClientHandler.AutomaticDecompression" />. Pair it with <c>IHttpClientFactory</c> through
/// <c>UseLyoHttpClientHandler</c>. New instance per handler lifetime; do not register as a singleton.
/// Swapping the primary handler (proxy, client certificates, test stubs) drops decompression unless the replacement subclasses this type or sets
/// <see cref="HttpClientHandler.AutomaticDecompression" /> itself. Do not also wrap a decompressing <see cref="DelegatingHandler" />; the body would inflate twice.
/// </summary>
public class LyoHttpClientHandler : HttpClientHandler
{
    /// <summary>Builds a handler from <paramref name="options" />. If <see cref="LyoHttpClientOptions.EnableAutoResponseDecompression" /> is false, decompression stays off.</summary>
    public LyoHttpClientHandler(LyoHttpClientOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        UseCookies = false;
        if (options.EnableAutoResponseDecompression)
            AutomaticDecompression = ToDecompressionMethods(options.AcceptEncodings);
        if (!string.IsNullOrWhiteSpace(options.ProxyUrl)) {
            UseProxy = true;
            var proxy = new WebProxy(options.ProxyUrl);
            if (!string.IsNullOrWhiteSpace(options.ProxyUsername))
                proxy.Credentials = new NetworkCredential(options.ProxyUsername, options.ProxyPassword ?? "");
            Proxy = proxy;
        }
    }

    /// <summary>
    /// Maps <see cref="LyoContentEncodings" /> tokens (<c>gzip</c>, <c>deflate</c>, <c>br</c>) onto <see cref="DecompressionMethods" /> flags. Unknown values are skipped.
    /// </summary>
    public static DecompressionMethods ToDecompressionMethods(IEnumerable<string>? encodings)
    {
        var methods = DecompressionMethods.None;
        if (encodings == null)
            return methods;

        foreach (var raw in encodings) {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var encoding = raw.Trim().ToLowerInvariant();
            if (encoding == LyoContentEncodings.GZip)
                methods |= DecompressionMethods.GZip;
            else if (encoding == LyoContentEncodings.Deflate)
                methods |= DecompressionMethods.Deflate;
#if !NETSTANDARD2_0
            else if (encoding == LyoContentEncodings.Brotli)
                methods |= DecompressionMethods.Brotli;
#endif
        }

        return methods;
    }

    /// <summary>True when <paramref name="encoding" /> is gzip, deflate, or (on modern TFMs) brotli.</summary>
    public static bool IsSupportedResponseEncoding(string encoding)
    {
        if (encoding is LyoContentEncodings.GZip or LyoContentEncodings.Deflate)
            return true;
#if !NETSTANDARD2_0
        if (encoding == LyoContentEncodings.Brotli)
            return true;
#endif
        return false;
    }
}
