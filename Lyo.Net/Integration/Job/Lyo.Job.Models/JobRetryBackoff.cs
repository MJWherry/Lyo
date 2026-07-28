using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models;

/// <summary>Computes retry delay seconds from a definition's backoff settings.</summary>
public static class JobRetryBackoff
{
    private static readonly Random JitterRandom = new();

    /// <summary>
    /// Returns the delay in seconds before the given retry <paramref name="attempt" /> (1-based). Linear: <c>baseSeconds × attempt</c>. Exponential:
    /// <c>baseSeconds × 2^(attempt-1)</c> with ±25% random jitter.
    /// </summary>
    public static int ComputeBackoffSeconds(int baseSeconds, int attempt, JobRetryBackoffType type)
    {
        if (baseSeconds <= 0 || attempt <= 0)
            return 0;

        var delay = type switch {
            JobRetryBackoffType.Exponential => baseSeconds * (1 << (attempt - 1)),
            var _ => baseSeconds * attempt
        };

        if (type != JobRetryBackoffType.Exponential)
            return delay;

        // ±25% jitter to reduce thundering herds on exponential retries.
        var jitterFactor = 0.75 + JitterRandom.NextDouble() * 0.5;
        return Math.Max(1, (int)Math.Round(delay * jitterFactor));
    }
}