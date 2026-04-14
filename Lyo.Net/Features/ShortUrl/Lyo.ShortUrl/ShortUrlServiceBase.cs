using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.ShortUrl.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.ShortUrl.ShortUrlErrorCodes;

namespace Lyo.ShortUrl;

/// <summary>Base class for URL shortener service implementations.</summary>
public class ShortUrlServiceBase : IShortUrlService
{
    /// <summary>Gets the logger instance.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the URL shortener service options.</summary>
    protected ShortUrlServiceOptions Options { get; }

    /// <summary>Gets the metrics instance (null if metrics are disabled).</summary>
    protected IMetrics Metrics { get; }

    /// <summary>Gets the metric names dictionary. Derived classes can modify this dictionary to override metric names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Initializes a new instance of the <see cref="ShortUrlServiceBase" /> class.</summary>
    /// <param name="options">The URL shortener service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance for tracking URL shortener operations.</param>
    protected ShortUrlServiceBase(ShortUrlServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <inheritdoc />
    public async Task<UrlShortenResult> ShortenAsync(string longUrl, string? customAlias = null, DateTime? expirationDate = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(longUrl, nameof(longUrl));

        // Enforce HTTPS if option is enabled
        if (Options.EnforceHttps && Uri.TryCreate(longUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttp) {
            var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port == 80 ? -1 : uri.Port };
            longUrl = builder.Uri.ToString();
            Logger.LogDebug("Converted HTTP URL to HTTPS: {Url}", longUrl);
        }

        ArgumentHelpers.ThrowIf(expirationDate.HasValue && expirationDate!.Value <= DateTime.UtcNow, "Expiration date must be in the future.", nameof(expirationDate));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.ShortenDuration)]);
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        try {
            var result = await ShortenCoreAsync(longUrl, customAlias, expirationDate, ct).ConfigureAwait(false);
            sw.Stop();
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.ShortenSuccess)]);
            return result;
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            Logger.LogWarning(ex, "URL shorten operation was cancelled");
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.ShortenCancelled)]);
            return UrlShortenResult.FromException(ex, longUrl, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to shorten URL: {LongUrl}", longUrl);
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.ShortenFailure)]);
            Metrics.RecordError(MetricNames[nameof(Constants.Metrics.ShortenDuration)], ex);
            return UrlShortenResult.FromException(ex, longUrl, ShortenFailed);
        }
    }

    /// <inheritdoc />
    public Task<UrlShortenResult> ShortenAsync(UrlShortenBuilder builder, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(builder, nameof(builder));
        var (longUrl, customAlias, expirationDate) = builder.Build();
        return ShortenAsync(longUrl, customAlias, expirationDate, ct);
    }

    /// <inheritdoc />
    public async Task<UrlExpandResult> ExpandAsync(string shortUrl, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(shortUrl, nameof(shortUrl));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.ExpandDuration)]);
        var sw = Stopwatch.StartNew();
        ct.ThrowIfCancellationRequested();
        try {
            var result = await ExpandCoreAsync(shortUrl, ct).ConfigureAwait(false);
            sw.Stop();
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.ExpandSuccess)]);
            return result;
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            Logger.LogWarning(ex, "URL expand operation was cancelled");
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.ExpandCancelled)]);
            return UrlExpandResult.FromException(ex, shortUrl, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to expand URL: {ShortUrl}", shortUrl);
            Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.ExpandFailure)]);
            Metrics.RecordError(MetricNames[nameof(Constants.Metrics.ExpandDuration)], ex);
            return UrlExpandResult.FromException(ex, shortUrl, ExpandFailed);
        }
    }

    /// <inheritdoc />
    public virtual Task<UrlStatisticsResult> GetStatisticsAsync(string shortUrl, CancellationToken ct = default)
        => throw new NotSupportedException("GetStatisticsAsync is not supported by this implementation.");

    /// <inheritdoc />
    public virtual Task<bool> DeleteAsync(string shortUrl, CancellationToken ct = default)
        => throw new NotSupportedException("DeleteAsync is not supported by this implementation.");

    /// <inheritdoc />
    public virtual Task<UrlShortenResult> UpdateAsync(string shortUrl, string newLongUrl, CancellationToken ct = default)
        => throw new NotSupportedException("UpdateAsync is not supported by this implementation.");

    /// <inheritdoc />
    public virtual Task<bool> TestConnectionAsync(CancellationToken ct = default)
        => throw new NotSupportedException("TestConnectionAsync is not supported by this implementation.");

    /// <summary>Creates the metric names dictionary. Override in derived classes to customize metric names.</summary>
    protected virtual Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Constants.Metrics.ShortenDuration), Constants.Metrics.ShortenDuration },
            { nameof(Constants.Metrics.ShortenSuccess), Constants.Metrics.ShortenSuccess },
            { nameof(Constants.Metrics.ShortenFailure), Constants.Metrics.ShortenFailure },
            { nameof(Constants.Metrics.ShortenCancelled), Constants.Metrics.ShortenCancelled },
            { nameof(Constants.Metrics.ExpandDuration), Constants.Metrics.ExpandDuration },
            { nameof(Constants.Metrics.ExpandSuccess), Constants.Metrics.ExpandSuccess },
            { nameof(Constants.Metrics.ExpandFailure), Constants.Metrics.ExpandFailure },
            { nameof(Constants.Metrics.ExpandCancelled), Constants.Metrics.ExpandCancelled },
            { nameof(Constants.Metrics.StatisticsDuration), Constants.Metrics.StatisticsDuration },
            { nameof(Constants.Metrics.DeleteDuration), Constants.Metrics.DeleteDuration },
            { nameof(Constants.Metrics.UpdateDuration), Constants.Metrics.UpdateDuration }
        };

    /// <summary>Shortens a URL.</summary>
    protected virtual Task<UrlShortenResult> ShortenCoreAsync(string longUrl, string? customAlias, DateTime? expirationDate, CancellationToken ct)
        => throw new NotSupportedException("ShortenCoreAsync is not supported by this implementation.");

    /// <summary>Expands a short URL.</summary>
    protected virtual Task<UrlExpandResult> ExpandCoreAsync(string shortUrl, CancellationToken ct)
        => throw new NotSupportedException("ExpandCoreAsync is not supported by this implementation.");
}