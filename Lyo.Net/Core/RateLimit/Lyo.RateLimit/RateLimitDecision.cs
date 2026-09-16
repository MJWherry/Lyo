namespace Lyo.RateLimit;

/// <summary>Result of a non-blocking acquire.</summary>
/// <param name="Allowed">True when a permit was taken.</param>
/// <param name="RetryAfter">Wait before the next permit. Null when <paramref name="Allowed" /> is true.</param>
/// <param name="Limit">Permit limit the decision was made against.</param>
/// <param name="Remaining">Permits left in the current window after this call. Zero when denied.</param>
public readonly record struct RateLimitDecision(bool Allowed, TimeSpan? RetryAfter, int Limit, long Remaining);
