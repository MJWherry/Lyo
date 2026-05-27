using Lyo.Authentication.Audit;
using Lyo.Diagnostic.Correlation;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Lyo.Authentication.AspNetCore.Audit;

/// <summary>
/// ASP.NET Core implementation of <see cref="IAuthAuditContextAccessor"/>. Sources IP and User-Agent from the ambient <see cref="HttpContext"/> via
/// <see cref="IHttpContextAccessor"/>, and the correlation id from the injected <see cref="ICorrelationIdResolver"/> so the audit row matches the value used elsewhere (outbound
/// HTTP stamping, structured logs, diagnostic request metadata). Returns <c>null</c> for any field that isn't populated (no inbound request, header missing, etc.) so the
/// recorder still produces a row instead of empty strings.
/// </summary>
public sealed class HttpAuthAuditContextAccessor : IAuthAuditContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICorrelationIdResolver _correlationIdResolver;

    /// <summary>Creates a new accessor.</summary>
    public HttpAuthAuditContextAccessor(IHttpContextAccessor httpContextAccessor, ICorrelationIdResolver correlationIdResolver)
    {
        ArgumentHelpers.ThrowIfNull(httpContextAccessor);
        ArgumentHelpers.ThrowIfNull(correlationIdResolver);
        _httpContextAccessor = httpContextAccessor;
        _correlationIdResolver = correlationIdResolver;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns <see cref="ConnectionInfo.RemoteIpAddress"/> as-is. Hosts behind a reverse proxy should configure <c>UseForwardedHeaders</c> so this reflects the client IP rather
    /// than the proxy.
    /// </remarks>
    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc/>
    public string? UserAgent
    {
        get {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
                return null;

            var ua = ctx.Request.Headers.UserAgent.ToString();
            return string.IsNullOrEmpty(ua) ? null : ua;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to the injected <see cref="ICorrelationIdResolver"/>. With <c>Lyo.Diagnostic.AspNetCore</c> wired up, this walks <c>DiagnosticWebOptions.CorrelationIdHeaders</c>
    /// against the inbound request, then falls back to <c>HttpContext.TraceIdentifier</c>, <c>Activity.Current</c>'s id, and finally a fresh GUID. Hosts that don't reference the
    /// diagnostics package get the ambient fallback registered by <c>AddLyoBearerAuthentication</c>.
    /// </remarks>
    public string? CorrelationId
    {
        get {
            var id = _correlationIdResolver.Resolve();
            return string.IsNullOrEmpty(id) ? null : id;
        }
    }
}
