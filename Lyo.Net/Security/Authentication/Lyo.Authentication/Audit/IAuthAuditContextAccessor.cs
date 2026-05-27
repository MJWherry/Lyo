using Lyo.Authentication.Models.Audit;

namespace Lyo.Authentication.Audit;

/// <summary>
/// Per-request enricher for <see cref="AuthAuditEvent"/> values: provides whatever ambient context (IP, user-agent, correlation id) the current host knows about. Recorders consult
/// this when their caller didn't supply those fields explicitly. The default <see cref="NullAuthAuditContextAccessor"/> returns nothing.
/// </summary>
public interface IAuthAuditContextAccessor
{
    /// <summary>The caller's IP address, when one exists.</summary>
    string? IpAddress { get; }

    /// <summary>The caller's User-Agent header, when one exists.</summary>
    string? UserAgent { get; }

    /// <summary>The current request's correlation id (W3C traceparent or whatever the host uses), when one exists.</summary>
    string? CorrelationId { get; }
}

/// <summary>Default <see cref="IAuthAuditContextAccessor"/> that knows nothing. Registered when the host hasn't supplied a more capable accessor (e.g. an HTTP one).</summary>
public sealed class NullAuthAuditContextAccessor : IAuthAuditContextAccessor
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullAuthAuditContextAccessor Instance = new();

    /// <inheritdoc/>
    public string? IpAddress => null;

    /// <inheritdoc/>
    public string? UserAgent => null;

    /// <inheritdoc/>
    public string? CorrelationId => null;
}
