using System.Diagnostics;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.ShortUrl;

/// <summary>Fluent builder for constructing URL shortening requests with validation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class UrlShortenBuilder
{
    private static readonly Regex AliasRegex = new(@"^[a-zA-Z0-9\-]+$", RegexOptions.Compiled);
    private string? _customAlias;
    private DateTime? _expirationDate;
    private string? _longUrl;

    /// <summary>Assigns the long URL to shorten.</summary>
    /// <param name="longUrl">Long URL being shortened.</param>
    /// <param name="enforceHttps">When true, HTTP URLs become HTTPS. Default: false</param>
    /// <returns>Same builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Raised when the URL is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Raised when the URL is malformed.</exception>
    public UrlShortenBuilder SetLongUrl(string longUrl, bool enforceHttps = false)
    {
        var uri = UriHelpers.GetValidWebUri(longUrl);

        // Upgrade to HTTPS when that option is on
        if (enforceHttps && uri.Scheme == Uri.UriSchemeHttp) {
            var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port == 80 ? -1 : uri.Port };
            _longUrl = builder.Uri.ToString();
        }
        else
            _longUrl = uri.ToString();

        return this;
    }

    /// <summary>Assigns a custom alias/slug for the short URL.</summary>
    /// <param name="alias">Custom alias; letters, digits, and hyphens only.</param>
    /// <returns>Same builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Raised when the alias fails validation.</exception>
    public UrlShortenBuilder SetCustomAlias(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) {
            _customAlias = null;
            return this;
        }

        FormatHelpers.ThrowIfInvalidFormat(alias, AliasRegex, "Custom alias must contain only alphanumeric characters and hyphens: {0}", "Alphanumeric and hyphens only");
        _customAlias = alias;
        return this;
    }

    /// <summary>Assigns the expiration date for the short URL.</summary>
    /// <param name="expirationDate">Expiry instant; must be in the future.</param>
    /// <returns>Same builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Raised when the expiry is already in the past.</exception>
    public UrlShortenBuilder SetExpirationDate(DateTime? expirationDate)
    {
        ArgumentHelpers.ThrowIf(expirationDate.HasValue && expirationDate.Value <= DateTime.UtcNow, "Expiration date must be in the future.", nameof(expirationDate));
        _expirationDate = expirationDate;
        return this;
    }

    /// <summary>Resets every builder field.</summary>
    /// <returns>Same builder so further calls can chain.</returns>
    public UrlShortenBuilder Clear()
    {
        _longUrl = null;
        _customAlias = null;
        _expirationDate = null;
        return this;
    }

    /// <summary>Assembles the shorten request.</summary>
    /// <returns>Tuple of long URL, custom alias, and expiry.</returns>
    /// <exception cref="InvalidOperationException">Raised when a required field is missing.</exception>
    public (string LongUrl, string? CustomAlias, DateTime? ExpirationDate) Build()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_longUrl);
        return (_longUrl, _customAlias, _expirationDate);
    }

    /// <summary>Constructs a new instance of UrlShortenBuilder.</summary>
    /// <returns>Fresh UrlShortenBuilder.</returns>
    public static UrlShortenBuilder New() => new();

    public override string ToString()
        => $"URL Shorten: LongUrl={_longUrl ?? "(not set)"}, Alias={_customAlias ?? "(not set)"}, Expiration={_expirationDate?.ToString("g") ?? "(not set)"}";
}