using System.Globalization;
using Lyo.Exceptions;
using StackExchange.Redis;

namespace Lyo.RateLimit.Redis;

/// <summary>Quota store on Redis. Every host that shares a quota must use this registration. A process still on <see cref="LocalRateLimitService" /> does not share these counters.</summary>
public sealed class RedisRateLimitService : IRateLimitService
{
    private readonly RedisRateLimitOptions _options;
    private readonly IRateLimitRedisExecutor _redis;

    /// <summary>Creates a store on <paramref name="multiplexer" />.</summary>
    public RedisRateLimitService(IConnectionMultiplexer multiplexer, RedisRateLimitOptions options)
        : this(CreateExecutor(multiplexer), options) { }

    internal RedisRateLimitService(IRateLimitRedisExecutor redis, RedisRateLimitOptions options)
    {
        ArgumentHelpers.ThrowIfNull(redis);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _redis = redis;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<RateLimitDecision> TryAcquireAsync(RateLimitRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Validate(request);
        var windowMs = (long)request.Window.TotalMilliseconds;
        var key = RateLimitKeys.Build(request.Policy, request.PartitionKey, request.Pattern, windowMs);
        var script = request.Pattern switch {
            RateLimitWindow.Fixed => RedisRateLimitScripts.Fixed,
            RateLimitWindow.Sliding => RedisRateLimitScripts.Sliding,
            RateLimitWindow.TokenBucket => RedisRateLimitScripts.TokenBucket,
            var _ => throw new ArgumentException("Unknown rate-limit window.", nameof(request))
        };

        var args = new RedisValue[] {
            request.NowUnixMilliseconds.ToString(CultureInfo.InvariantCulture),
            request.PermitLimit.ToString(CultureInfo.InvariantCulture),
            windowMs.ToString(CultureInfo.InvariantCulture)
        };

        long[] values;
        try {
            values = await _redis.EvaluateAsync(script, key, args, ct).ConfigureAwait(false);
        }
        catch (RedisTimeoutException) {
            return StoreFailure(request);
        }
        catch (RedisException) {
            return StoreFailure(request);
        }

        if (values.Length < 5)
            return StoreFailure(request);

        var allowed = values[0] == 1;
        var now = values[1];
        var remaining = values[2];
        if (remaining < 0)
            return new(false, request.Window, request.PermitLimit, 0);

        if (allowed)
            return new(true, null, request.PermitLimit, remaining);

        var retryMs = request.Pattern switch {
            RateLimitWindow.Fixed => RateLimitMath.FixedRetryAfterMs(now, windowMs),
            RateLimitWindow.Sliding => RateLimitMath.SlidingRetryAfterMs(values[3], values[4], PositiveMod(now, windowMs), windowMs, request.PermitLimit),
            var _ => RateLimitMath.TokenRetryAfterMs(values[3], request.PermitLimit, windowMs)
        };

        return new(false, TimeSpan.FromMilliseconds(retryMs), request.PermitLimit, 0);
    }

    private static IRateLimitRedisExecutor CreateExecutor(IConnectionMultiplexer multiplexer)
    {
        ArgumentHelpers.ThrowIfNull(multiplexer);
        return new StackExchangeRateLimitRedis(multiplexer);
    }

    private RateLimitDecision StoreFailure(RateLimitRequest request)
    {
        if (_options.FailOpen)
            return new(true, null, request.PermitLimit, request.PermitLimit);

        return new(false, request.Window, request.PermitLimit, 0);
    }

    private static long PositiveMod(long now, long windowMs)
    {
        var mod = now % windowMs;
        return mod < 0 ? mod + windowMs : mod;
    }

    private static void Validate(RateLimitRequest request)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Policy);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.PartitionKey);
        ArgumentHelpers.ThrowIf(request.PermitLimit < 1, "PermitLimit must be at least 1.");
        ArgumentHelpers.ThrowIf(request.Window.TotalMilliseconds < 1, "Window must be at least 1ms.");
        ArgumentHelpers.ThrowIf(request.NowUnixMilliseconds < 0, "NowUnixMilliseconds must be zero or positive.");
    }
}