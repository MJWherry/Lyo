using Lyo.ShortUrl.Models;

namespace Lyo.ShortUrl;

/// <summary>Contract for URL shortening operations.</summary>
public interface IShortUrlService
{
    /// <summary>Produces a short form of a URL.</summary>
    /// <param name="longUrl">Long URL being shortened.</param>
    /// <param name="customAlias">Optional custom slug for the short URL.</param>
    /// <param name="expirationDate">Optional expiry for the short URL.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Result that carries the short URL.</returns>
    Task<UrlShortenResult> ShortenAsync(string longUrl, string? customAlias = null, DateTime? expirationDate = null, CancellationToken ct = default);

    /// <summary>Produces a short form of a URL using a builder.</summary>
    /// <param name="builder">Builder that holds the shortening request.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Result that carries the short URL.</returns>
    Task<UrlShortenResult> ShortenAsync(UrlShortenBuilder builder, CancellationToken ct = default);

    /// <summary>Resolves a short URL to get the original URL.</summary>
    /// <param name="shortUrl">Short URL to resolve.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Result that carries the original URL.</returns>
    Task<UrlExpandResult> ExpandAsync(string shortUrl, CancellationToken ct = default);

    /// <summary>Returns statistics for a short URL (clicks, creation date, etc.).</summary>
    /// <param name="shortUrl">Short URL whose stats are requested.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Statistics payload.</returns>
    Task<UrlStatisticsResult> GetStatisticsAsync(string shortUrl, CancellationToken ct = default);

    /// <summary>Removes a short URL.</summary>
    /// <param name="shortUrl">Short URL that should be removed.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>True on a successful delete; otherwise false.</returns>
    Task<bool> DeleteAsync(string shortUrl, CancellationToken ct = default);

    /// <summary>Changes a short URL (e.g., change destination, expiration).</summary>
    /// <param name="shortUrl">Short URL whose fields change.</param>
    /// <param name="newLongUrl">Replacement long URL.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Result after the update.</returns>
    Task<UrlShortenResult> UpdateAsync(string shortUrl, string newLongUrl, CancellationToken ct = default);

    /// <summary>Probes the connection to the URL shortener service.</summary>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>True when the connection check succeeds; otherwise false.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}