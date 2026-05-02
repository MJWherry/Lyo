using Lyo.Diagnostic.Context;
using Microsoft.AspNetCore.Http;

namespace Lyo.Diagnostic.AspNetCore;

/// <summary>Builds <see cref="RequestMetadata" /> from the current HTTP context.</summary>
public static class DiagnosticHttpContextExtensions
{
    /// <summary>Maps correlation id, method, path, query, identity, client IP, and user-agent into request metadata.</summary>
    public static RequestMetadata ToDiagnosticRequestMetadata(this HttpContext context, DiagnosticWebOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        string? correlation = null;
        foreach (var headerName in options.CorrelationIdHeaders) {
            if (context.Request.Headers.TryGetValue(headerName, out var v) && !Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(v)) {
                correlation = v.ToString();
                break;
            }
        }

        correlation ??= context.TraceIdentifier;

        var path = context.Request.Path.HasValue ? context.Request.Path.Value : null;
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null;
        var user = context.User?.Identity is { IsAuthenticated: true } ? context.User.Identity?.Name : null;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var ua = context.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrEmpty(ua))
            ua = null;

        return new(correlation, context.Request.Method, path, query, user, ip, ua, new Dictionary<string, string>());
    }
}
