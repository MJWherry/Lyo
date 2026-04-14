using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.ShortUrl.Models;
using Microsoft.Extensions.Logging;
using static Lyo.ShortUrl.ShortUrlErrorCodes;

namespace Lyo.ShortUrl;

/// <summary>Concrete implementation of URL shortener service that generates short URLs.</summary>
public sealed class ShortUrlService : ShortUrlServiceBase
{
    private const int DefaultShortIdLength = 8;
    private readonly IShortUrlGenerator _urlGenerator;

    /// <summary>Initializes a new instance of the <see cref="ShortUrlService" /> class.</summary>
    /// <param name="options">The URL shortener service options.</param>
    /// <param name="urlGenerator">The short URL generator service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance for tracking URL shortener operations.</param>
    public ShortUrlService(ShortUrlServiceOptions options, IShortUrlGenerator urlGenerator, ILogger<ShortUrlService>? logger = null, IMetrics? metrics = null)
        : base(options, logger, metrics)
    {
        ArgumentHelpers.ThrowIfNull(urlGenerator, nameof(urlGenerator));
        _urlGenerator = urlGenerator;
    }

    /// <summary>Shortens a URL.</summary>
    protected override Task<UrlShortenResult> ShortenCoreAsync(string longUrl, string? customAlias, DateTime? expirationDate, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        try {
            // Validate custom alias if provided
            if (!string.IsNullOrWhiteSpace(customAlias)) {
                if (!Options.AllowCustomAliases) {
                    sw.Stop();
                    return Task.FromResult(
                        UrlShortenResult.FromError("Custom aliases are not allowed", CustomAliasNotAllowed, longUrl, new InvalidOperationException("Custom aliases are disabled")));
                }

                if (customAlias!.Length < Options.MinAliasLength || customAlias.Length > Options.MaxAliasLength) {
                    sw.Stop();
                    return Task.FromResult(
                        UrlShortenResult.FromError(
                            $"Custom alias must be between {Options.MinAliasLength} and {Options.MaxAliasLength} characters", InvalidAliasLength, longUrl,
                            new ArgumentException($"Invalid alias length: {customAlias.Length}")));
                }
            }

            // Generate ID if custom alias not provided
            var id = customAlias ?? _urlGenerator.Generate(DefaultShortIdLength);
            sw.Stop();
            var shortUrl = BuildShortUrl(id);
            Logger.LogInformation("Generated short URL: {ShortUrl} -> {LongUrl}", shortUrl, longUrl);
            return Task.FromResult(UrlShortenResult.FromSuccess(shortUrl, longUrl, customAlias, DateTime.UtcNow, expirationDate, $"URL shortened successfully: {shortUrl}"));
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            return Task.FromResult(UrlShortenResult.FromException(ex, longUrl, OperationCancelled));
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to shorten URL: {LongUrl}", longUrl);
            return Task.FromResult(UrlShortenResult.FromException(ex, longUrl, ShortenFailed));
        }
    }

    /// <summary>Expands a short URL (not supported without storage).</summary>
    protected override Task<UrlExpandResult> ExpandCoreAsync(string shortUrl, CancellationToken ct)
        => Task.FromResult(
            UrlExpandResult.FromError(
                "URL expansion is not supported. Storage must be implemented separately.", ExpandFailed, shortUrl,
                new NotSupportedException("URL expansion requires storage implementation")));

    /// <summary>Gets statistics for a short URL (not supported without storage).</summary>
    public override Task<UrlStatisticsResult> GetStatisticsAsync(string shortUrl, CancellationToken ct = default)
        => Task.FromResult(
            UrlStatisticsResult.FromError(
                "Statistics are not supported. Storage must be implemented separately.", GetStatisticsFailed, shortUrl,
                new NotSupportedException("Statistics require storage implementation")));

    /// <summary>Deletes a short URL (not supported without storage).</summary>
    public override Task<bool> DeleteAsync(string shortUrl, CancellationToken ct = default) => throw new NotSupportedException("URL deletion requires storage implementation.");

    /// <summary>Updates a short URL (not supported without storage).</summary>
    public override Task<UrlShortenResult> UpdateAsync(string shortUrl, string newLongUrl, CancellationToken ct = default)
        => Task.FromResult(
            UrlShortenResult.FromError(
                "URL update is not supported. Storage must be implemented separately.", UpdateFailed, newLongUrl,
                new NotSupportedException("URL update requires storage implementation")));

    /// <summary>Tests the connection (always returns true for URL generation).</summary>
    public override Task<bool> TestConnectionAsync(CancellationToken ct = default) => Task.FromResult(true);

    /// <summary>Builds the full short URL from an ID.</summary>
    private string BuildShortUrl(string id)
    {
        if (!string.IsNullOrWhiteSpace(Options.BaseUrl))
            return $"{Options.BaseUrl.TrimEnd('/')}/{id}";

        return id; // Return just the ID if no base URL provided
    }
}