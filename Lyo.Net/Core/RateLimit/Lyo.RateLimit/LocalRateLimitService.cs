using System.Collections.Concurrent;
using Lyo.Exceptions;

namespace Lyo.RateLimit;

/// <summary>In-memory quotas for one process. A per-key lock makes concurrent acquires on the same partition share one count.</summary>
public sealed class LocalRateLimitService : IRateLimitService
{
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, Bucket> _buckets = new(StringComparer.Ordinal);

    /// <summary>Creates a store that reads <paramref name="options" />.<see cref="RateLimitOptions.Clock" /> when a request omits the time.</summary>
    public LocalRateLimitService(RateLimitOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    /// <inheritdoc />
    public ValueTask<RateLimitDecision> TryAcquireAsync(RateLimitRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return new(TryAcquire(request));
    }

    private RateLimitDecision TryAcquire(RateLimitRequest request)
    {
        Validate(request);
        var windowMs = (long)request.Window.TotalMilliseconds;
        var now = ResolveNow(request);
        var key = RateLimitKeys.Build(request.Policy, request.PartitionKey, request.Pattern, windowMs);
        var bucket = _buckets.GetOrAdd(key, static _ => new Bucket());
        RateLimitDecision decision;
        lock (bucket.Gate) {
            if (bucket.Seen && now - bucket.LastTouchMs > windowMs * 2)
                bucket.Reset();

            if (bucket.Seen && bucket.PermitLimit != request.PermitLimit)
                return Denied(request.PermitLimit, request.Window);

            bucket.Seen = true;
            bucket.PermitLimit = request.PermitLimit;
            bucket.LastTouchMs = now;
            decision = request.Pattern switch {
                RateLimitWindow.Fixed => AcquireFixed(bucket, request, now, windowMs),
                RateLimitWindow.Sliding => AcquireSliding(bucket, request, now, windowMs),
                RateLimitWindow.TokenBucket => AcquireToken(bucket, request, now, windowMs),
                _ => throw new ArgumentException("Unknown rate-limit window.", nameof(request))
            };
        }

        EvictIfNeeded();
        return decision;
    }

    private static RateLimitDecision AcquireFixed(Bucket bucket, RateLimitRequest request, long now, long windowMs)
    {
        var windowId = now / windowMs;
        if (bucket.HasWindow && windowId < bucket.WindowId)
            return Denied(request.PermitLimit, request.Window);

        if (!bucket.HasWindow || windowId != bucket.WindowId) {
            bucket.WindowId = windowId;
            bucket.Count = 0;
            bucket.HasWindow = true;
        }

        bucket.Count++;
        var allowed = bucket.Count <= request.PermitLimit;
        var remaining = request.PermitLimit - bucket.Count;
        if (remaining < 0)
            remaining = 0;

        if (allowed)
            return Allowed(request.PermitLimit, remaining);

        return Denied(request.PermitLimit, TimeSpan.FromMilliseconds(RateLimitMath.FixedRetryAfterMs(now, windowMs)));
    }

    private static RateLimitDecision AcquireSliding(Bucket bucket, RateLimitRequest request, long now, long windowMs)
    {
        var windowId = now / windowMs;
        var elapsed = now % windowMs;
        if (bucket.HasWindow && windowId < bucket.WindowId)
            return Denied(request.PermitLimit, request.Window);

        if (!bucket.HasWindow) {
            bucket.Previous = 0;
            bucket.Current = 0;
        }
        else if (windowId == bucket.WindowId + 1) {
            bucket.Previous = bucket.Current;
            bucket.Current = 0;
        }
        else if (windowId > bucket.WindowId + 1) {
            bucket.Previous = 0;
            bucket.Current = 0;
        }

        bucket.WindowId = windowId;
        bucket.HasWindow = true;
        var allowed = RateLimitMath.SlidingAllows(bucket.Previous, bucket.Current, elapsed, windowMs, request.PermitLimit);
        if (!allowed) {
            var retry = RateLimitMath.SlidingRetryAfterMs(bucket.Previous, bucket.Current, elapsed, windowMs, request.PermitLimit);
            return Denied(request.PermitLimit, TimeSpan.FromMilliseconds(retry));
        }

        bucket.Current++;
        var used = RateLimitMath.SlidingUsedMilli(bucket.Previous, elapsed, windowMs) + (bucket.Current * 1000L);
        var remaining = ((request.PermitLimit * 1000L) - used) / 1000L;
        if (remaining < 0)
            remaining = 0;

        return Allowed(request.PermitLimit, remaining);
    }

    private static RateLimitDecision AcquireToken(Bucket bucket, RateLimitRequest request, long now, long windowMs)
    {
        var full = request.PermitLimit * 1000L;
        if (!bucket.HasWindow) {
            bucket.Tokens = full;
            bucket.TimestampMs = now;
            bucket.HasWindow = true;
        }
        else {
            RateLimitMath.TryRefillToken(ref bucket.Tokens, ref bucket.TimestampMs, now, request.PermitLimit, windowMs);
        }

        if (bucket.Tokens < 1000) {
            var retry = RateLimitMath.TokenRetryAfterMs(bucket.Tokens, request.PermitLimit, windowMs);
            return Denied(request.PermitLimit, TimeSpan.FromMilliseconds(retry));
        }

        bucket.Tokens -= 1000;
        return Allowed(request.PermitLimit, bucket.Tokens / 1000L);
    }

    private void EvictIfNeeded()
    {
        var extra = _buckets.Count - _options.MaxEntries;
        if (extra <= 0)
            return;

        var oldest = _buckets
            .OrderBy(static pair => pair.Value.LastTouchMs)
            .Take(extra)
            .Select(static pair => pair.Key)
            .ToArray();

        foreach (var key in oldest)
            _buckets.TryRemove(key, out _);
    }

    private long ResolveNow(RateLimitRequest request)
    {
        if (request.NowUnixMilliseconds > 0)
            return request.NowUnixMilliseconds;

        return _options.Clock.GetUtcNow().ToUnixTimeMilliseconds();
    }

    private static void Validate(RateLimitRequest request)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Policy);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.PartitionKey);
        ArgumentHelpers.ThrowIf(request.PermitLimit < 1, "PermitLimit must be at least 1.");
        ArgumentHelpers.ThrowIf(request.Window.TotalMilliseconds < 1, "Window must be at least 1ms.");
        ArgumentHelpers.ThrowIf(request.NowUnixMilliseconds < 0, "NowUnixMilliseconds must be zero or positive.");
    }

    private static RateLimitDecision Allowed(int limit, long remaining) => new(true, null, limit, remaining);

    private static RateLimitDecision Denied(int limit, TimeSpan retryAfter) => new(false, retryAfter, limit, 0);

    private sealed class Bucket
    {
        public readonly object Gate = new();
        public bool Seen;
        public bool HasWindow;
        public int PermitLimit;
        public long LastTouchMs;
        public long WindowId;
        public long Count;
        public long Previous;
        public long Current;
        public long Tokens;
        public long TimestampMs;

        public void Reset()
        {
            Seen = false;
            HasWindow = false;
            Count = 0;
            Previous = 0;
            Current = 0;
            Tokens = 0;
            TimestampMs = 0;
            WindowId = 0;
        }
    }
}
