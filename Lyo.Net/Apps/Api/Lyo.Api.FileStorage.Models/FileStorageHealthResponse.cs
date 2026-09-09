using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>GET <c>health</c> body. JSON-safe slice of <c>Lyo.Health.HealthResult</c> (drops <c>Exception</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorageHealthResponse(bool IsHealthy, string? Message)
{
    /// <inheritdoc />
    public override string ToString() => $"FileStorageHealthResponse: IsHealthy={IsHealthy}{(Message == null ? "" : $", {Message}")}";
}
