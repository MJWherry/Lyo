using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>GET <c>health</c> payload. JSON-safe subset of <c>Lyo.Health.HealthResult</c> (no <c>Exception</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorageHealthResponse(bool IsHealthy, string? Message)
{
    /// <inheritdoc />
    public override string ToString() => $"FileStorageHealthResponse: IsHealthy={IsHealthy}{(Message == null ? "" : $", {Message}")}";
}
