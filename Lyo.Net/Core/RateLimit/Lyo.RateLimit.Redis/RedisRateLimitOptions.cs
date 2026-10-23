using System.Diagnostics;

namespace Lyo.RateLimit.Redis;

/// <summary>Settings for <see cref="RedisRateLimitService" />. The connection string is passed to <c>AddRedisRateLimit</c>, not stored here.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class RedisRateLimitOptions : RateLimitOptions
{
    /// <summary>Fail-open flag and partition cap from <see cref="RateLimitOptions" />.</summary>
    public override string ToString() => base.ToString();
}