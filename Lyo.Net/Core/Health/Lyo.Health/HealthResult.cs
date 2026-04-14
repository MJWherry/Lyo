namespace Lyo.Health;

/// <summary>Result of a health check. Includes status, timings, and optional metadata.</summary>
public sealed class HealthResult
{
    /// <summary>Whether the check passed.</summary>
    public bool IsHealthy { get; }

    /// <summary>Duration of the health check.</summary>
    public TimeSpan Duration { get; }

    /// <summary>When the check was performed.</summary>
    public DateTime CheckedAt { get; }

    /// <summary>Optional message or description.</summary>
    public string? Message { get; }

    /// <summary>Optional metadata (e.g. connection info, version, system-specific data).</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; }

    /// <summary>Exception if the check failed with an error.</summary>
    public Exception? Exception { get; }

    /// <summary>Creates a new HealthResult.</summary>
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

    /// <summary>Creates a healthy result.</summary>
    public static HealthResult Healthy(TimeSpan duration, string? message = null, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(true, duration, DateTime.UtcNow, message, metadata);

    /// <summary>Creates an unhealthy result.</summary>
    public static HealthResult Unhealthy(TimeSpan duration, string? message = null, IReadOnlyDictionary<string, object?>? metadata = null, Exception? exception = null)
        => new(false, duration, DateTime.UtcNow, message, metadata, exception);
}