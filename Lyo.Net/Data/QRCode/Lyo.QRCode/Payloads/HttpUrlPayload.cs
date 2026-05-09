using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary>Absolute HTTP(S) URL for QR content.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class HttpUrlPayload : IQrPayload
{
    /// <summary>Creates a payload from a URL string.</summary>
    /// <param name="url">Absolute http or https URL.</param>
    /// <param name="forceHttps">If true and the URL uses <c>http:</c>, it is rewritten to <c>https:</c> (same host/path/query).</param>
    public HttpUrlPayload(string url, bool forceHttps = false)
    {
        ArgumentHelpers.ThrowIfNull(url);
        Url = url.Trim();
        ForceHttps = forceHttps;
    }

    /// <summary>Creates a payload from an absolute <see cref="Uri" />.</summary>
    public HttpUrlPayload(Uri uri, bool forceHttps = false)
    {
        ArgumentHelpers.ThrowIfNull(uri);
        ArgumentHelpers.ThrowIf(!uri.IsAbsoluteUri, "URI must be absolute.", nameof(uri));

        Url = uri.ToString();
        ForceHttps = forceHttps;
    }

    /// <summary>Original URL text (trimmed).</summary>
    public string Url { get; }

    /// <summary>Whether to upgrade <c>http</c> to <c>https</c>.</summary>
    public bool ForceHttps { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var u = Url.Length <= 72 ? Url : Url[..72] + "…";
        return $"HttpUrlPayload forceHttps={ForceHttps}, {u}";
    }

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(Url), "URL cannot be empty.", nameof(Url));

        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri))
            throw new InvalidFormatException("URL is not a valid absolute URI.", nameof(Url), Url, "https://example.com/path");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidFormatException("URL must use http or https.", nameof(Url), Url, "http://…", "https://…");

        if (!ForceHttps || uri.Scheme == Uri.UriSchemeHttps)
            return uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);

        var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 };
        return builder.Uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
    }
}
