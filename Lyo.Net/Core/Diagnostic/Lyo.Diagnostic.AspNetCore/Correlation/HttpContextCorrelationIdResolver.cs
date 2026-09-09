using System.Diagnostics;
using Lyo.Diagnostic.Correlation;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Http;
namespace Lyo.Diagnostic.AspNetCore.Correlation;

/// <summary>
/// ASP.NET Core <see cref="ICorrelationIdResolver" />. Walks <see cref="DiagnosticWebOptions.CorrelationIdHeaders" /> against the inbound <see cref="HttpRequest" />, then
/// falls back (in order) to <see cref="HttpContext.TraceIdentifier" />, <see cref="Activity.Current" />'s id, and finally a fresh hex GUID when no <see cref="HttpContext" /> is
/// in scope. Reading the same options as <see cref="DiagnosticHttpContextExtensions.ToDiagnosticRequestMetadata" /> keeps the diagnostic enricher, the auth audit, and outbound
/// stamping agreed on which header is authoritative.
/// </summary>
public sealed class HttpContextCorrelationIdResolver : ICorrelationIdResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DiagnosticWebOptions _options;

    /// <summary>Mints a resolver that reads the current <see cref="HttpContext" />.</summary>
    public HttpContextCorrelationIdResolver(IHttpContextAccessor httpContextAccessor, DiagnosticWebOptions options)
    {
        ArgumentHelpers.ThrowIfNull(httpContextAccessor);
        ArgumentHelpers.ThrowIfNull(options);
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    /// <inheritdoc />
    public string Resolve()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null)
            return DiagnosticHttpContextExtensions.ResolveCorrelationId(ctx, _options);

        var activityId = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(activityId))
            return activityId!;

        return Guid.NewGuid().ToString("N");
    }
}