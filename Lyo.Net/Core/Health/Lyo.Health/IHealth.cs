namespace Lyo.Health;

/// <summary>Interface for services that can report their health. Services implement or extend this to expose health with metadata and timings.</summary>
public interface IHealth
{
    /// <summary>Name for this health check (e.g. "filestorage", "cache", "rabbitmq").</summary>
    string HealthCheckName { get; }

    /// <summary>Checks the service health. Returns a result with status, timings, and optional metadata.</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Health result with status, duration, and metadata</returns>
    Task<HealthResult> CheckHealthAsync(CancellationToken ct = default);
}