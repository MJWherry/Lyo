namespace Lyo.Job.Models.Enums;

/// <summary>How the delay between automatic retry attempts grows.</summary>
public enum JobRetryBackoffType
{
    /// <summary>Delay grows linearly: <c>RetryBackoffSeconds × attempt</c>.</summary>
    Linear = 0,

    /// <summary>Delay grows exponentially with jitter: <c>RetryBackoffSeconds × 2^(attempt-1)</c> ± up to 20% random jitter.</summary>
    Exponential = 1
}
