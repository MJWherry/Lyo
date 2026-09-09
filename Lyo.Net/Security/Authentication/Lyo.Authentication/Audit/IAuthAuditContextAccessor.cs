using Lyo.Authentication.Models.Audit;

namespace Lyo.Authentication.Audit;

/// <summary>
/// Per-request enricher for <see cref="AuthAuditEvent" />: supplies ambient context (IP, user-agent, correlation id) the current host knows. Recorders
/// consult this when the caller omitted those fields. The default <see cref="NullAuthAuditContextAccessor" /> returns nothing.
/// </summary>
public interface IAuthAuditContextAccessor
{
    /// <summary>Caller IP address, when one exists.</summary>
    string? IpAddress { get; }

    /// <summary>Caller User-Agent header, when one exists.</summary>
    string? UserAgent { get; }

    /// <summary>Current request correlation id (W3C traceparent or whatever the host uses), when one exists.</summary>
    string? CorrelationId { get; }
}

/// <summary>Default <see cref="IAuthAuditContextAccessor" /> that knows nothing. Registered when the host has not supplied a richer accessor (HTTP, for example).</summary>
public sealed class NullAuthAuditContextAccessor : IAuthAuditContextAccessor
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullAuthAuditContextAccessor Instance = new();

    /// <inheritdoc />
    public string? IpAddress => null;

    /// <inheritdoc />
    public string? UserAgent => null;

    /// <inheritdoc />
    public string? CorrelationId => null;
}