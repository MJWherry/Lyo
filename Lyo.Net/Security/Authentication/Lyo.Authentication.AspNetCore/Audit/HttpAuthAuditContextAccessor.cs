using Lyo.Authentication.Audit;
using Lyo.Common.Core.Extensions;
using Lyo.Diagnostic.Correlation;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Lyo.Authentication.AspNetCore.Audit;

/// <summary>
/// ASP.NET Core <see cref="IAuthAuditContextAccessor" />. IP and User-Agent come from the ambient <see cref="HttpContext" /> through
/// <see cref="IHttpContextAccessor" />; the correlation id comes from <see cref="ICorrelationIdResolver" /> so the audit row matches outbound
/// HTTP stamps, structured logs, and diagnostic request metadata. Any missing field is <c>null</c> (no inbound request, absent header, and so on) so the recorder
/// still writes a row instead of empty strings.
/// </summary>
public sealed class HttpAuthAuditContextAccessor : IAuthAuditContextAccessor
{
    private readonly ICorrelationIdResolver _correlationIdResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Builds a new accessor.</summary>
    public HttpAuthAuditContextAccessor(IHttpContextAccessor httpContextAccessor, ICorrelationIdResolver correlationIdResolver)
    {
        ArgumentHelpers.ThrowIfNull(httpContextAccessor);
        ArgumentHelpers.ThrowIfNull(correlationIdResolver);
        _httpContextAccessor = httpContextAccessor;
        _correlationIdResolver = correlationIdResolver;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see cref="ConnectionInfo.RemoteIpAddress" /> unchanged. Hosts behind a reverse proxy should turn on <c>UseForwardedHeaders</c> so this is the client IP
    /// rather than the proxy.
    /// </remarks>
    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public string? UserAgent {
        get {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
                return null;

            var ua = ctx.Request.Headers.UserAgent.ToString();
            return ua.IsNullOrEmpty() ? null : ua;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Forwards to the injected <see cref="ICorrelationIdResolver" />. With <c>Lyo.Diagnostic.AspNetCore</c> wired, this walks
    /// <c>DiagnosticWebOptions.CorrelationIdHeaders</c> on the inbound request, then <c>HttpContext.TraceIdentifier</c>, <c>Activity.Current</c>'s id, and last a
    /// new GUID. Hosts without the diagnostics package get the ambient fallback from <c>AddLyoBearerAuthentication</c>.
    /// </remarks>
    public string? CorrelationId {
        get {
            var id = _correlationIdResolver.Resolve();
            return id.IsNullOrEmpty() ? null : id;
        }
    }
}