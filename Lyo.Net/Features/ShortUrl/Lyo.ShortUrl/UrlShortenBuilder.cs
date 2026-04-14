using System.Diagnostics;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.ShortUrl;

/// <summary>Builder class for constructing URL shortening requests with validation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class UrlShortenBuilder
{
    private static readonly Regex AliasRegex = new(@"^[a-zA-Z0-9\-]+$", RegexOptions.Compiled);
    private string? _customAlias;
    private DateTime? _expirationDate;
    private string? _longUrl;

    /// <summary>Sets the long URL to shorten.</summary>
    /// <param name="longUrl">The original URL to shorten.</param>
    /// <param name="enforceHttps">If true, converts HTTP URLs to HTTPS. Default: false</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when URL is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when URL format is invalid.</exception>
    public UrlShortenBuilder SetLongUrl(string longUrl, bool enforceHttps = false)
    {
        var uri = UriHelpers.GetValidWebUri(longUrl, nameof(longUrl));

        // Enforce HTTPS if requested
        if (enforceHttps && uri.Scheme == Uri.UriSchemeHttp) {
            var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port == 80 ? -1 : uri.Port };
            _longUrl = builder.Uri.ToString();
        }
        else
            _longUrl = uri.ToString();

        return this;
    }

    /// <summary>Sets a custom alias/slug for the short URL.</summary>
    /// <param name="alias">The custom alias (alphanumeric and hyphens only).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when alias is invalid.</exception>
    public UrlShortenBuilder SetCustomAlias(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) {
            _customAlias = null;
            return this;
        }

        FormatHelpers.ThrowIfInvalidFormat(
            alias, AliasRegex, "Custom alias must contain only alphanumeric characters and hyphens: {0}", nameof(alias), "Alphanumeric and hyphens only");

        _customAlias = alias;
        return this;
    }

    /// <summary>Sets the expiration date for the short URL.</summary>
    /// <param name="expirationDate">The expiration date (must be in the future).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when expiration date is in the past.</exception>
    public UrlShortenBuilder SetExpirationDate(DateTime? expirationDate)
    {
        ArgumentHelpers.ThrowIf(expirationDate.HasValue && expirationDate!.Value <= DateTime.UtcNow, "Expiration date must be in the future.", nameof(expirationDate));
        _expirationDate = expirationDate;
        return this;
    }

    /// <summary>Clears all builder properties.</summary>
    /// <returns>The builder instance for method chaining.</returns>
    public UrlShortenBuilder Clear()
    {
        _longUrl = null;
        _customAlias = null;
        _expirationDate = null;
        return this;
    }

    /// <summary>Builds the URL shortening request.</summary>
    /// <returns>A tuple containing the long URL, custom alias, and expiration date.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
    public (string LongUrl, string? CustomAlias, DateTime? ExpirationDate) Build()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_longUrl, nameof(_longUrl));
        return (_longUrl, _customAlias, _expirationDate);
    }

    /// <summary>Creates a new instance of UrlShortenBuilder.</summary>
    /// <returns>A new UrlShortenBuilder instance.</returns>
    public static UrlShortenBuilder New() => new();

    public override string ToString()
        => $"URL Shorten: LongUrl={_longUrl ?? "(not set)"}, Alias={_customAlias ?? "(not set)"}, Expiration={_expirationDate?.ToString("g") ?? "(not set)"}";
}