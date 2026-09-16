using StackExchange.Redis;

namespace Lyo.RateLimit.Redis;

/// <summary>Script runner over an existing multiplexer. One key per quota, so the script never spans slots.</summary>
internal sealed class StackExchangeRateLimitRedis(IConnectionMultiplexer multiplexer) : IRateLimitRedisExecutor
{
    /// <inheritdoc />
    public async Task<long[]> EvaluateAsync(string script, string key, RedisValue[] args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var raw = await multiplexer.GetDatabase().ScriptEvaluateAsync(script, [(RedisKey)key], args).ConfigureAwait(false);
        if (raw.IsNull)
            return [];

        var values = (RedisResult[])raw!;
        var parsed = new long[values.Length];
        for (var i = 0; i < values.Length; i++)
            parsed[i] = (long)values[i];

        return parsed;
    }
}
