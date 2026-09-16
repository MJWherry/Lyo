using Lyo.RateLimit;

namespace Lyo.RateLimit.Redis;

/// <summary>Settings for <see cref="RedisRateLimitService" />. The connection string is passed to <c>AddRedisRateLimit</c>, not stored here.</summary>
public sealed class RedisRateLimitOptions : RateLimitOptions;
