using Microsoft.AspNetCore.Http;

namespace Lyo.Webhook;

/// <summary>Reads raw bytes for signature verification (must match the exact payload the sender signed).</summary>
public static class HttpRequestWebhookExtensions
{
    /// <summary>Buffers and reads the full request body, then resets the stream position so later middleware or model binding can read again.</summary>
    public static async Task<ReadOnlyMemory<byte>> ReadRawBodyAsync(this HttpRequest request, CancellationToken cancellationToken = default)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        using (var ms = new MemoryStream((int)Math.Min(request.ContentLength ?? 8192, int.MaxValue))) {
            await request.Body.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            request.Body.Position = 0;
            return ms.ToArray();
        }
    }

    /// <summary>Builds a case-insensitive header dictionary suitable for <see cref="WebhookVerificationContext.Headers" />.</summary>
    public static Dictionary<string, string> ToWebhookHeaderDictionary(this IHeaderDictionary headers)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in headers) {
            var s = kv.Value.ToString();
            if (s.Length > 0)
                d[kv.Key] = s;
        }

        return d;
    }

    /// <summary>Resolves the public request URL (scheme + host + path + query). Prefer forwarded headers when behind a reverse proxy.</summary>
    public static string GetPublicRequestUrl(this HttpRequest request) => $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
}