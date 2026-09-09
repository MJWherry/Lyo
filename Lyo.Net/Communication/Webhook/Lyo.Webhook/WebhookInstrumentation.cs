using Lyo.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Webhook;

/// <summary>Resolves <see cref="IMetrics" /> and <see cref="ILogger" /> from the request, falling back to no-ops.</summary>
public static class WebhookInstrumentation
{
    /// <summary>Logger category name: <c>Lyo.Webhook</c>.</summary>
    public const string LoggerCategory = "Lyo.Webhook";

    /// <summary>Uses the registered <see cref="IMetrics" /> or <see cref="NullMetrics.Instance" />.</summary>
    public static IMetrics ResolveMetrics(HttpContext http) => http.RequestServices.GetService(typeof(IMetrics)) as IMetrics ?? NullMetrics.Instance;

    /// <summary>Uses the registered <see cref="ILoggerFactory" /> or <see cref="NullLogger" />.</summary>
    public static ILogger ResolveLogger(HttpContext http)
    {
        var factory = http.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        return factory != null ? factory.CreateLogger(LoggerCategory) : NullLogger.Instance;
    }
}