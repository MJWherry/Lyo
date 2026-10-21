using Lyo.Exceptions;

namespace Lyo.RateLimit;

/// <summary>Settings for <see cref="LocalRateLimitService" />. Redis options subclass this.</summary>
public class RateLimitOptions
{
    /// <summary>Configuration section bound by <c>AddLocalRateLimitFromConfiguration</c>.</summary>
    public const string SectionName = "RateLimit";

    /// <summary>Clock used when <see cref="RateLimitRequest.NowUnixMilliseconds" /> is zero. Tests replace this.</summary>
    public TimeProvider Clock { get; set; } = TimeProvider.System;

    /// <summary>
    /// When true, a Redis failure allows the call and writes nothing. Default false: a Redis failure is a denial with <see cref="RateLimitRequest.Window" /> as the retry.
    /// Ignored by the in-memory store.
    /// </summary>
    public bool FailOpen { get; set; }

    /// <summary>Most in-memory partitions kept. Older idle entries are dropped past this cap.</summary>
    public int MaxEntries { get; set; } = 100_000;

    /// <summary>Throws when <see cref="Clock" /> is missing or <see cref="MaxEntries" /> is not positive.</summary>
    public virtual void Validate()
    {
        ArgumentHelpers.ThrowIfNull(Clock);
        ArgumentHelpers.ThrowIf(MaxEntries <= 0, "MaxEntries must be greater than zero.");
    }
}