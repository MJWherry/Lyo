using Lyo.ShortUrl.Models;

namespace Lyo.ShortUrl;

/// <summary>Service interface for URL shortening operations.</summary>
public interface IShortUrlService
{
    /// <summary>Shortens a URL.</summary>
    /// <param name="longUrl">The original URL to shorten.</param>
    /// <param name="customAlias">Optional custom alias/slug for the short URL.</param>
    /// <param name="expirationDate">Optional expiration date for the short URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result containing the short URL.</returns>
    Task<UrlShortenResult> ShortenAsync(string longUrl, string? customAlias = null, DateTime? expirationDate = null, CancellationToken ct = default);

    /// <summary>Shortens a URL using a builder.</summary>
    /// <param name="builder">The URL builder containing URL details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result containing the short URL.</returns>
    Task<UrlShortenResult> ShortenAsync(UrlShortenBuilder builder, CancellationToken ct = default);

    /// <summary>Expands a short URL to get the original URL.</summary>
    /// <param name="shortUrl">The short URL to expand.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result containing the original URL.</returns>
    Task<UrlExpandResult> ExpandAsync(string shortUrl, CancellationToken ct = default);

    /// <summary>Gets statistics for a short URL (clicks, creation date, etc.).</summary>
    /// <param name="shortUrl">The short URL to get statistics for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The statistics result.</returns>
    Task<UrlStatisticsResult> GetStatisticsAsync(string shortUrl, CancellationToken ct = default);

    /// <summary>Deletes a short URL.</summary>
    /// <param name="shortUrl">The short URL to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted successfully, false otherwise.</returns>
    Task<bool> DeleteAsync(string shortUrl, CancellationToken ct = default);

    /// <summary>Updates a short URL (e.g., change destination, expiration).</summary>
    /// <param name="shortUrl">The short URL to update.</param>
    /// <param name="newLongUrl">The new destination URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated result.</returns>
    Task<UrlShortenResult> UpdateAsync(string shortUrl, string newLongUrl, CancellationToken ct = default);

    /// <summary>Tests the connection to the URL shortener service.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}