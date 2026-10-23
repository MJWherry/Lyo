using System.Diagnostics;

namespace Lyo.RateLimit;

/// <summary>One permit against a named quota. The store does not queue or wait.</summary>
/// <param name="Policy">Quota name. Callers that must not share a bucket use different names.</param>
/// <param name="PartitionKey">Caller key inside the policy (user, token, host). Not a secret.</param>
/// <param name="PermitLimit">Permits restored each <paramref name="Window" />. The first observed value for a key wins.</param>
/// <param name="Window">Restore period. Must be at least one millisecond.</param>
/// <param name="Pattern">Window rule.</param>
/// <param name="NowUnixMilliseconds">Unix milliseconds. Zero uses the store clock (Redis <c>TIME</c> when the store is Redis).</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RateLimitRequest(string Policy, string PartitionKey, int PermitLimit, TimeSpan Window, RateLimitWindow Pattern, long NowUnixMilliseconds = 0);