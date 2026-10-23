namespace Lyo.RateLimit;

/// <summary>
/// Atomic permit check for one process or, with <c>Lyo.RateLimit.Redis</c>, every host that shares the quota. Never waits. Callers that must wait sleep on
/// <see cref="RateLimitDecision.RetryAfter" /> and call again.
/// </summary>
public interface IRateLimitService
{
    /// <summary>Takes one permit or returns a denial. A cancelled <paramref name="ct" /> throws <see cref="OperationCanceledException" /> and does not change the bucket.</summary>
    /// <param name="request">Quota, partition, and clock.</param>
    /// <param name="ct">Cancels before the bucket is touched.</param>
    ValueTask<RateLimitDecision> TryAcquireAsync(RateLimitRequest request, CancellationToken ct = default);
}