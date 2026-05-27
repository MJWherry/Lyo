using System.Diagnostics;
using Lyo.Diagnostic.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Lyo.Diagnostic.AspNetCore;

/// <summary>Builds <see cref="RequestMetadata" /> from the current HTTP context.</summary>
public static class DiagnosticHttpContextExtensions
{
    /// <summary>Maps correlation id, method, path, query, identity, client IP, and user-agent into request metadata.</summary>
    public static RequestMetadata ToDiagnosticRequestMetadata(this HttpContext context, DiagnosticWebOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        var correlation = ResolveCorrelationId(context, options);

        var path = context.Request.Path.HasValue ? context.Request.Path.Value : null;
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null;
        var user = context.User?.Identity is { IsAuthenticated: true } ? context.User.Identity?.Name : null;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var ua = context.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrEmpty(ua))
            ua = null;

        return new(correlation, context.Request.Method, path, query, user, ip, ua, new Dictionary<string, string>());
    }

    /// <summary>
    /// Walks <paramref name="options"/>.<see cref="DiagnosticWebOptions.CorrelationIdHeaders"/> against the inbound <paramref name="context"/>, then falls back to
    /// <see cref="HttpContext.TraceIdentifier"/>, then <see cref="Activity.Current"/>'s id, then a fresh hex GUID. Shared with <c>HttpContextCorrelationIdResolver</c> so the
    /// diagnostic enricher, the auth audit, and outbound stamping all agree on which header is authoritative.
    /// </summary>
    internal static string ResolveCorrelationId(HttpContext context, DiagnosticWebOptions options)
    {
        foreach (var headerName in options.CorrelationIdHeaders) {
            if (string.IsNullOrWhiteSpace(headerName))
                continue;

            if (context.Request.Headers.TryGetValue(headerName, out var value) && !StringValues.IsNullOrEmpty(value)) {
                var first = value.ToString();
                if (!string.IsNullOrWhiteSpace(first))
                    return first;
            }
        }

        if (!string.IsNullOrEmpty(context.TraceIdentifier))
            return context.TraceIdentifier;

        var activityId = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(activityId))
            return activityId!;

        return Guid.NewGuid().ToString("N");
    }
}
