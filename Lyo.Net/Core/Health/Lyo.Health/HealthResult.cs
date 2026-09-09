namespace Lyo.Health;

/// <summary>Outcome of a health probe: pass/fail, elapsed time, and optional extra fields.</summary>
public sealed class HealthResult
{
    /// <summary>True when the probe succeeded.</summary>
    public bool IsHealthy { get; }

    /// <summary>How long the probe took.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Clock time at which the probe ran.</summary>
    public DateTime CheckedAt { get; }

    /// <summary>Optional description or note.</summary>
    public string? Message { get; }

    /// <summary>Optional extra fields (connection details, version, host-specific data, and similar).</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; }

    /// <summary>Fault that caused a failed probe, when one was thrown.</summary>
    public Exception? Exception { get; }

    /// <summary>Builds a health-check outcome.</summary>
    public HealthResult(
        bool isHealthy,
        TimeSpan duration,
        DateTime checkedAt,
        string? message = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        Exception? exception = null)
    {
        IsHealthy = isHealthy;
        Duration = duration;
        CheckedAt = checkedAt;
        Message = message;
        Metadata = metadata;
        Exception = exception;
    }

    /// <summary>Builds a passing outcome.</summary>
    public static HealthResult Healthy(TimeSpan duration, string? message = null, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(true, duration, DateTime.UtcNow, message, metadata);

    /// <summary>Builds a failing outcome.</summary>
    public static HealthResult Unhealthy(TimeSpan duration, string? message = null, IReadOnlyDictionary<string, object?>? metadata = null, Exception? exception = null)
        => new(false, duration, DateTime.UtcNow, message, metadata, exception);
}