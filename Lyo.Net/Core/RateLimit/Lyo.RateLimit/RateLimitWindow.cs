namespace Lyo.RateLimit;

/// <summary>How a quota refills. Local and Redis stores use the same rules for a given value.</summary>
public enum RateLimitWindow
{
    /// <summary>Clock-aligned counter. A full window can be spent again at the next boundary.</summary>
    Fixed = 0,

    /// <summary>Weighted previous window plus the current window. A spent window stays closed at the rollover.</summary>
    Sliding = 1,

    /// <summary>Continuous refill from the last update, capped at <c>PermitLimit</c>.</summary>
    TokenBucket = 2
}
