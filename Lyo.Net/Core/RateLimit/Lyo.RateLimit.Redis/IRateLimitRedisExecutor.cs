using StackExchange.Redis;

namespace Lyo.RateLimit.Redis;

/// <summary>Runs one quota script. Tests substitute a double that throws before any write.</summary>
internal interface IRateLimitRedisExecutor
{
    /// <summary>Evaluates <paramref name="script" /> against one key. Returns five integers: allowed, now, remaining, state A, state B.</summary>
    Task<long[]> EvaluateAsync(string script, string key, RedisValue[] args, CancellationToken ct);
}
