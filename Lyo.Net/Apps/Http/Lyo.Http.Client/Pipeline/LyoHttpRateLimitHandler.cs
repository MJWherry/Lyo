using System.Threading.RateLimiting;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.RateLimit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Http.Client.Pipeline;

/// <summary>
/// Acquires a quota permit, then on HTTP 429 honors <c>Retry-After</c> (capped, jittered) up to <see cref="LyoHttpRateLimitOptions.MaxRetriesOn429" />.
/// Sits outside the timing handler: acquire, then delay, then send. A whitelisted path skips the quota and still honors the concurrency cap.
/// </summary>
public sealed class LyoHttpRateLimitHandler : DelegatingHandler
{
    private readonly LyoHttpRateLimitOptions _options;
    private readonly IRateLimitService? _limiter;
    private readonly string _policyName;
    private readonly LyoHttpRateLimitGate _gate;
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;
    private readonly Random _rng = new();

    /// <summary>Creates the handler. <paramref name="limiter" /> is required when <paramref name="options" /> are enabled.</summary>
    public LyoHttpRateLimitHandler(
        LyoHttpRateLimitOptions options,
        IRateLimitService? limiter,
        string policyName,
        LyoHttpRateLimitGate gate,
        IMetrics? metrics = null,
        ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(gate);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(policyName);
        if (options.Enabled && limiter is null)
            throw new InvalidOperationException("Rate limiting is enabled but IRateLimitService is not registered. Call AddLocalRateLimit or AddRedisRateLimit.");

        _options = options;
        _limiter = limiter;
        _policyName = policyName;
        _gate = gate;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Applies jitter in <c>1 ± Jitter</c> to <paramref name="wait" />.</summary>
    public TimeSpan JitterWait(TimeSpan wait, Random rng)
    {
        ArgumentHelpers.ThrowIfNull(rng);
        if (wait <= TimeSpan.Zero || _options.Jitter <= 0)
            return wait;

        var factor = 1 + ((_options.Jitter * 2) * rng.NextDouble() - _options.Jitter);
        var ms = Math.Max(0, wait.TotalMilliseconds * factor);
        return TimeSpan.FromMilliseconds(ms);
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var concurrency = await AcquireConcurrencyAsync(ct).ConfigureAwait(false);
        try {
            if (!_options.Enabled || IsWhitelisted(request))
                return await SendWithRetriesAsync(request, acquire: false, ct).ConfigureAwait(false);

            if (!await _gate.TryEnterAsync(ct).ConfigureAwait(false)) {
                _metrics.IncrementCounter("http.client.rate_limit.rejected", tags: [("reason", "queue")]);
                throw new RateLimitExceededException("HTTP client rate-limit queue is full.");
            }

            try {
                return await SendWithRetriesAsync(request, acquire: true, ct).ConfigureAwait(false);
            }
            finally {
                _gate.Exit();
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

    private async Task<HttpResponseMessage> SendWithRetriesAsync(HttpRequestMessage request, bool acquire, CancellationToken ct)
    {
        var host = request.RequestUri?.Host ?? "unknown";
        var attempts = 0;
        while (true) {
            attempts++;
            if (acquire)
                await WaitForPermitAsync(host, ct).ConfigureAwait(false);

            var response = await base.SendAsync(request, ct).ConfigureAwait(false);
            if ((int)response.StatusCode != 429 || !_options.HonorRetryAfter || attempts > _options.MaxRetriesOn429)
                return response;

            var retryAfter = ParseRetryAfter(response) ?? _options.Window;
            if (retryAfter > _options.RetryAfterMaxWait)
                retryAfter = _options.RetryAfterMaxWait;

            retryAfter = JitterWait(retryAfter, _rng);
            _metrics.IncrementCounter("http.client.rate_limit.rejected", tags: [("reason", "429")]);
            _metrics.RecordTiming("http.client.rate_limit.wait", retryAfter, [("host", host)]);
            _logger.LogDebug("HTTP 429 from {Host}; waiting {Delay} then retry {Attempt}", host, retryAfter, attempts);
            response.Dispose();
            await Task.Delay(retryAfter, ct).ConfigureAwait(false);
        }
    }

    private async Task WaitForPermitAsync(string host, CancellationToken ct)
    {
        var partition = _options.PartitionByHost ? host : "default";
        while (true) {
            ct.ThrowIfCancellationRequested();
            var decision = await _limiter!.TryAcquireAsync(
                    new RateLimitRequest(_policyName, partition, _options.PermitLimit, _options.Window, RateLimitWindow.TokenBucket), ct)
                .ConfigureAwait(false);
            if (decision.Allowed)
                return;

            var wait = decision.RetryAfter ?? _options.Window;
            if (wait < TimeSpan.Zero)
                wait = TimeSpan.Zero;

            await Task.Delay(wait, ct).ConfigureAwait(false);
        }
    }

    private bool IsWhitelisted(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? "/";
        return RateLimitEndpointMatcher.IsWhitelisted(_options.Whitelist, null, request.Method.Method, null, path);
    }

    private async Task<RateLimitLease?> AcquireConcurrencyAsync(CancellationToken ct)
    {
        if (!_options.Enabled || _gate.Concurrency is null)
            return null;

        var lease = await _gate.Concurrency.AcquireAsync(1, ct).ConfigureAwait(false);
        if (lease.IsAcquired)
            return lease;

        lease.Dispose();
        _metrics.IncrementCounter("http.client.rate_limit.rejected", tags: [("reason", "concurrency")]);
        throw new RateLimitExceededException("HTTP client concurrency limit reached.");
    }
}
