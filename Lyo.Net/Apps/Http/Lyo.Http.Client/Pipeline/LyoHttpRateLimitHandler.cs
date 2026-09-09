using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Http.Client.Pipeline;

/// <summary>Shared token buckets for one named client / options type. Handler instances are transient; this registry is a singleton so partitions survive pooling.</summary>
public sealed class LyoHttpRateLimiter
{
    private readonly LyoHttpRateLimitOptions _options;
    private readonly IMetrics _metrics;
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrencyLimiter? _concurrency;

    /// <summary>Creates a limiter from <paramref name="options" />.</summary>
    public LyoHttpRateLimiter(LyoHttpRateLimitOptions options, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
        _metrics = metrics ?? NullMetrics.Instance;
        if (options.MaxConcurrent > 0) {
            _concurrency = new(new() {
                PermitLimit = options.MaxConcurrent,
                QueueLimit = Math.Max(options.QueueLimit, options.MaxConcurrent)
            });
        }
    }

    /// <summary>Acquires an optional concurrency lease that the caller must dispose after the send completes.</summary>
    public async Task<RateLimitLease?> AcquireConcurrencyAsync(CancellationToken ct)
    {
        if (!_options.Enabled || _concurrency == null)
            return null;

        var lease = await _concurrency.AcquireAsync(1, ct).ConfigureAwait(false);
        if (lease.IsAcquired)
            return lease;

        lease.Dispose();
        _metrics.IncrementCounter("http.client.rate_limit.rejected", tags: [("reason", "concurrency")]);
        throw new RateLimitExceededException("HTTP client concurrency limit reached.");
    }

    /// <summary>Waits for a permit for <paramref name="partitionKey" /> (typically the request host).</summary>
    public Task AcquireAsync(string partitionKey, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(partitionKey);
        return !_options.Enabled ? Task.CompletedTask : AcquirePartitionAsync(partitionKey, ct);
    }

    /// <summary>Applies jitter in <c>1 ± Jitter</c> to <paramref name="wait" />.</summary>
    public TimeSpan JitterWait(TimeSpan wait, Random rng)
    {
        if (wait <= TimeSpan.Zero || _options.Jitter <= 0)
            return wait;

        var factor = 1 + ((_options.Jitter * 2) * rng.NextDouble() - _options.Jitter);
        var ms = Math.Max(0, wait.TotalMilliseconds * factor);
        return TimeSpan.FromMilliseconds(ms);
    }

    private async Task AcquirePartitionAsync(string partitionKey, CancellationToken ct)
    {
        var limiter = _limiters.GetOrAdd(partitionKey, _ => CreateLimiter());
        using var lease = await limiter.AcquireAsync(1, ct).ConfigureAwait(false);
        if (lease.IsAcquired)
            return;

        _metrics.IncrementCounter("http.client.rate_limit.rejected", tags: [("reason", "queue")]);
        throw new RateLimitExceededException("HTTP client rate-limit queue is full.");
    }

    private RateLimiter CreateLimiter()
        => new TokenBucketRateLimiter(new() {
            TokenLimit = _options.PermitLimit,
            TokensPerPeriod = _options.PermitLimit,
            ReplenishmentPeriod = _options.Window,
            QueueLimit = _options.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
}

/// <summary>
/// Acquires a rate-limit permit, then on HTTP 429 honors <c>Retry-After</c> (capped, jittered) up to <see cref="LyoHttpRateLimitOptions.MaxRetriesOn429" />.
/// Sits outside the timing handler: acquire, then delay, then send.
/// </summary>
public sealed class LyoHttpRateLimitHandler : DelegatingHandler
{
    private readonly LyoHttpRateLimitOptions _options;
    private readonly LyoHttpRateLimiter _limiter;
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;
    private readonly Random _rng = new();

    /// <summary>Creates the handler.</summary>
    public LyoHttpRateLimitHandler(LyoHttpRateLimitOptions options, LyoHttpRateLimiter limiter, IMetrics? metrics = null, ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(limiter);
        _options = options;
        _limiter = limiter;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var host = request.RequestUri?.Host ?? "unknown";
        var partition = _options.PartitionByHost ? host : "default";
        var concurrency = await _limiter.AcquireConcurrencyAsync(ct).ConfigureAwait(false);
        try {
            var attempts = 0;
            while (true) {
                attempts++;
                await _limiter.AcquireAsync(partition, ct).ConfigureAwait(false);
                var response = await base.SendAsync(request, ct).ConfigureAwait(false);
                if ((int)response.StatusCode != 429 || !_options.HonorRetryAfter || attempts > _options.MaxRetriesOn429)
                    return response;

                var retryAfter = ParseRetryAfter(response) ?? _options.Window;
                if (retryAfter > _options.RetryAfterMaxWait)
                    retryAfter = _options.RetryAfterMaxWait;

                retryAfter = _limiter.JitterWait(retryAfter, _rng);
                _metrics.IncrementCounter("http.client.rate_limit.rejected", tags: [("reason", "429")]);
                _metrics.RecordTiming("http.client.rate_limit.wait", retryAfter, [("host", host)]);
                _logger.LogDebug("HTTP 429 from {Host}; waiting {Delay} then retry {Attempt}", host, retryAfter, attempts);
                response.Dispose();
                await Task.Delay(retryAfter, ct).ConfigureAwait(false);
            }
        }
        finally {
            concurrency?.Dispose();
        }
    }

    internal static TimeSpan? ParseRetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        if (header == null)
            return null;

        if (header.Delta is { } delta)
            return delta;

        if (header.Date is { } date) {
            var wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }
}
