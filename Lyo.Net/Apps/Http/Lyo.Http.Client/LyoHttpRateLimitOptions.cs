using Lyo.Exceptions;

namespace Lyo.Http.Client;

/// <summary>Client-side token-bucket rate limit. Off or generous for vendors; Flared uses a strict bucket (for example 1 permit / 2s).</summary>
public sealed class LyoHttpRateLimitOptions
{
    /// <summary>When false, the rate-limit handler acquires no permits.</summary>
    public bool Enabled { get; set; }

    /// <summary>Tokens available at once (and replenished each <see cref="Window" />).</summary>
    public int PermitLimit { get; set; } = 30;

    /// <summary>Replenishment period. Generic default 1s; Flared typically 2s.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Queued acquire waiters before <c>RateLimitExceededException</c>.</summary>
    public int QueueLimit { get; set; } = 32;

    /// <summary>When true (default), each request host gets its own bucket.</summary>
    public bool PartitionByHost { get; set; } = true;

    /// <summary>On HTTP 429, wait using <c>Retry-After</c> (capped) and retry.</summary>
    public bool HonorRetryAfter { get; set; } = true;

    /// <summary>Cap on 429 waits so a huge Retry-After cannot stall the caller forever.</summary>
    public TimeSpan RetryAfterMaxWait { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Retries after 429 (not counting the first attempt).</summary>
    public int MaxRetriesOn429 { get; set; } = 2;

    /// <summary>± fraction applied to Retry-After and queue waits.</summary>
    public double Jitter { get; set; } = 0.2;

    /// <summary>Optional concurrency cap across in-flight requests (including downloads). 0 means unlimited.</summary>
    public int MaxConcurrent { get; set; }

    /// <summary>Throws when limits are not positive while enabled.</summary>
    public void Validate()
    {
        if (!Enabled)
            return;

        ArgumentHelpers.ThrowIf(PermitLimit <= 0, "PermitLimit must be > 0 when rate limiting is enabled.");
        ArgumentHelpers.ThrowIf(Window <= TimeSpan.Zero, "Window must be > 0 when rate limiting is enabled.");
        ArgumentHelpers.ThrowIf(QueueLimit < 0, "QueueLimit must be >= 0.");
        ArgumentHelpers.ThrowIf(MaxRetriesOn429 < 0, "MaxRetriesOn429 must be >= 0.");
        ArgumentHelpers.ThrowIf(Jitter < 0, "Jitter must be >= 0.");
        ArgumentHelpers.ThrowIf(MaxConcurrent < 0, "MaxConcurrent must be >= 0.");
    }
}
