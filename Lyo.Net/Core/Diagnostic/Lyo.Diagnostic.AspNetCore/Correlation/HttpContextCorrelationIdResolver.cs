using System;
using System.Diagnostics;
using Lyo.Diagnostic.Correlation;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Lyo.Diagnostic.AspNetCore.Correlation;

/// <summary>
/// ASP.NET Core <see cref="ICorrelationIdResolver"/>. Walks <see cref="DiagnosticWebOptions.CorrelationIdHeaders"/> against the inbound <see cref="HttpRequest"/>, then falls back
/// (in order) to <see cref="HttpContext.TraceIdentifier"/>, <see cref="Activity.Current"/>'s id, and finally a fresh hex GUID when no <see cref="HttpContext"/> is in scope.
/// Reading from the same options object as <see cref="DiagnosticHttpContextExtensions.ToDiagnosticRequestMetadata"/> guarantees that the diagnostic enricher, the auth audit, and
/// any outbound stamping all agree on which header is authoritative.
/// </summary>
public sealed class HttpContextCorrelationIdResolver : ICorrelationIdResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<DiagnosticWebOptions> _options;

    /// <summary>Creates a new resolver.</summary>
    public HttpContextCorrelationIdResolver(IHttpContextAccessor httpContextAccessor, IOptions<DiagnosticWebOptions> options)
    {
        ArgumentHelpers.ThrowIfNull(httpContextAccessor);
        ArgumentHelpers.ThrowIfNull(options);
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    /// <inheritdoc/>
    public string Resolve()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null)
            return DiagnosticHttpContextExtensions.ResolveCorrelationId(ctx, _options.Value);

        var activityId = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(activityId))
            return activityId!;

        return Guid.NewGuid().ToString("N");
    }
}
