namespace Lyo.Health;

/// <summary>A service that can report readiness. Implement or extend this to surface status, timing, and extra fields.</summary>
public interface IHealth
{
    /// <summary>Probe label (examples: "filestorage", "cache", "rabbitmq").</summary>
    string HealthCheckName { get; }

    /// <summary>Runs the probe and returns status, elapsed time, and any extra fields.</summary>
    /// <param name="ct">Token used to cancel the probe</param>
    /// <returns>Outcome with status, duration, and metadata</returns>
    Task<HealthResult> CheckHealthAsync(CancellationToken ct = default);
}