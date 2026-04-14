using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lyo.Webhook;

/// <summary>Fluent registration of verified webhook endpoints on <see cref="IEndpointRouteBuilder" /> (including <c>WebApplication</c>).</summary>
public static class WebhookEndpointRouteBuilderExtensions
{
    /// <summary>Maps a webhook route that accepts POST by default. Chain <see cref="WebhookEndpointMappingBuilder.Verify" /> then <see cref="VerifiedWebhookEndpointBuilder.Handle" />.</summary>
    public static WebhookEndpointMappingBuilder MapWebhook(this IEndpointRouteBuilder endpoints, string pattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        return new(endpoints, pattern, new[] { HttpMethods.Post });
    }

    /// <summary>Maps a webhook route for the given HTTP methods (e.g. <see cref="HttpMethods.Post" />).</summary>
    public static WebhookEndpointMappingBuilder MapWebhook(this IEndpointRouteBuilder endpoints, string pattern, IReadOnlyList<string> httpMethods)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(httpMethods);
        return new(endpoints, pattern, httpMethods);
    }
}