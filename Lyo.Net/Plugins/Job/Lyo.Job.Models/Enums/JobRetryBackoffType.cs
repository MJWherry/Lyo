namespace Lyo.Job.Models.Enums;

/// <summary>How the delay between automatic retry attempts increases.</summary>
public enum JobRetryBackoffType
{
    /// <summary>Delay increases linearly: <c>RetryBackoffSeconds × attempt</c>.</summary>
    Linear = 0,

    /// <summary>Delay increases exponentially with jitter: <c>RetryBackoffSeconds × 2^(attempt-1)</c> ± up to 20% random jitter.</summary>
    Exponential = 1
}