using System.Text;
using System.Text.Json;
using Lyo.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Lyo.Webhook;

/// <summary>
/// Starts a fluent webhook route. Call <see cref="Verify" /> then <see cref="VerifiedWebhookEndpointBuilder.Handle" /> or
/// <see cref="VerifiedWebhookEndpointBuilder.HandleJson{TRequest}" />.
/// </summary>
public sealed class WebhookEndpointMappingBuilder
{
    private readonly IEndpointRouteBuilder _endpoints;
    private readonly IReadOnlyList<string> _httpMethods;
    private readonly string _pattern;

    internal WebhookEndpointMappingBuilder(IEndpointRouteBuilder endpoints, string pattern, IReadOnlyList<string> httpMethods)
    {
        _endpoints = endpoints;
        _pattern = pattern;
        _httpMethods = httpMethods;
    }

    /// <summary>Registers the signature verifier used before your handler runs.</summary>
    public VerifiedWebhookEndpointBuilder Verify(IWebhookSignatureVerifier verifier)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        return new(_endpoints, _pattern, _httpMethods, verifier);
    }
}

/// <summary>Configure the handler after <see cref="WebhookEndpointMappingBuilder.Verify" />.</summary>
public sealed class VerifiedWebhookEndpointBuilder
{
    private readonly IEndpointRouteBuilder _endpoints;
    private readonly IReadOnlyList<string> _httpMethods;
    private readonly string _pattern;
    private readonly IWebhookSignatureVerifier _verifier;

    internal VerifiedWebhookEndpointBuilder(IEndpointRouteBuilder endpoints, string pattern, IReadOnlyList<string> httpMethods, IWebhookSignatureVerifier verifier)
    {
        _endpoints = endpoints;
        _pattern = pattern;
        _httpMethods = httpMethods;
        _verifier = verifier;
    }

    /// <summary>Maps a handler that receives the verified raw body.</summary>
    public RouteHandlerBuilder Handle(Func<WebhookHandlerContext, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return (RouteHandlerBuilder)_endpoints.MapMethods(
            _pattern, _httpMethods, async http => {
                await RunPipelineAsync(http, handler, null).ConfigureAwait(false);
            });
    }

    /// <summary>Maps a handler that receives the verified raw body and a cancellation token.</summary>
    public RouteHandlerBuilder Handle(Func<WebhookHandlerContext, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return (RouteHandlerBuilder)_endpoints.MapMethods(
            _pattern, _httpMethods, async http => {
                await RunPipelineAsync(http, ctx => handler(ctx, ctx.CancellationToken), null).ConfigureAwait(false);
            });
    }

    /// <summary>Maps a handler after verifying the signature and deserializing JSON to <typeparamref name="TRequest" />.</summary>
    public RouteHandlerBuilder HandleJson<TRequest>(Func<WebhookRequestContext<TRequest>, Task> handler, JsonSerializerOptions? jsonOptions = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return (RouteHandlerBuilder)_endpoints.MapMethods(
            _pattern, _httpMethods, async http => {
                await RunPipelineAsync(http, null, (body, metrics, logger) => HandleJsonAsync(http, body, handler, jsonOptions, metrics, logger)).ConfigureAwait(false);
            });
    }

    /// <summary>Maps a handler after verifying the signature and deserializing JSON to <typeparamref name="TRequest" />.</summary>
    public RouteHandlerBuilder HandleJson<TRequest>(Func<WebhookRequestContext<TRequest>, CancellationToken, Task> handler, JsonSerializerOptions? jsonOptions = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return (RouteHandlerBuilder)_endpoints.MapMethods(
            _pattern, _httpMethods, async http => {
                await RunPipelineAsync(
                        http, null, (body, metrics, logger) => HandleJsonAsync<TRequest>(http, body, ctx => handler(ctx, ctx.CancellationToken), jsonOptions, metrics, logger))
                    .ConfigureAwait(false);
            });
    }

    private async Task RunPipelineAsync(HttpContext http, Func<WebhookHandlerContext, Task>? rawHandler, Func<ReadOnlyMemory<byte>, IMetrics, ILogger, Task>? jsonHandler)
    {
        var metrics = WebhookInstrumentation.ResolveMetrics(http);
        var logger = WebhookInstrumentation.ResolveLogger(http);
        var routeTags = RouteTags();
        using (metrics.StartTimer(WebhookMetrics.RequestDuration, routeTags)) {
            var body = await http.Request.ReadRawBodyAsync(http.RequestAborted).ConfigureAwait(false);
            logger.LogDebug("Webhook request {Route}, body length {BodyLength}", _pattern, body.Length);
            WebhookVerificationContext verification;
            using (metrics.StartTimer(WebhookMetrics.VerificationDuration, routeTags)) {
                verification = BuildVerificationContext(http, body);
                var result = _verifier.Verify(verification);
                if (!result.Success) {
                    metrics.IncrementCounter(WebhookMetrics.VerificationFailed, tags: routeTags);
                    logger.LogWarning("Webhook verification failed for route {Route}: {Reason}", _pattern, result.FailureReason);
                    http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            metrics.IncrementCounter(WebhookMetrics.VerificationSucceeded, tags: routeTags);
            if (jsonHandler != null) {
                await jsonHandler(body, metrics, logger).ConfigureAwait(false);
                return;
            }

            if (rawHandler != null) {
                var ctx = new WebhookHandlerContext(http, body);
                try {
                    using (metrics.StartTimer(WebhookMetrics.HandlerDuration, routeTags))
                        await rawHandler(ctx).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    metrics.RecordError(WebhookMetrics.HandlerError, ex, routeTags);
                    logger.LogError(ex, "Webhook handler failed for route {Route}", _pattern);
                    throw;
                }
            }
        }
    }

    private async Task HandleJsonAsync<TRequest>(
        HttpContext http,
        ReadOnlyMemory<byte> body,
        Func<WebhookRequestContext<TRequest>, Task> handler,
        JsonSerializerOptions? jsonOptions,
        IMetrics metrics,
        ILogger logger)
    {
        var routeTags = RouteTags();
        TRequest? requestModel;
        try {
            requestModel = JsonSerializer.Deserialize<TRequest>(body.Span, jsonOptions ?? JsonSerializerOptions.Web);
        }
        catch (JsonException ex) {
            metrics.IncrementCounter(WebhookMetrics.JsonDeserializeFailed, tags: routeTags);
            logger.LogWarning(ex, "Webhook JSON deserialize failed for route {Route}", _pattern);
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (requestModel is null && typeof(TRequest).IsClass) {
            metrics.IncrementCounter(WebhookMetrics.JsonDeserializeFailed, tags: routeTags);
            logger.LogWarning("Webhook JSON deserialize returned null for route {Route}", _pattern);
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var ctx = new WebhookRequestContext<TRequest>(http, requestModel!, body);
        try {
            using (metrics.StartTimer(WebhookMetrics.HandlerDuration, routeTags))
                await handler(ctx).ConfigureAwait(false);
        }
        catch (Exception ex) {
            metrics.RecordError(WebhookMetrics.HandlerError, ex, routeTags);
            logger.LogError(ex, "Webhook handler failed for route {Route}", _pattern);
            throw;
        }
    }

    /// <summary>Builds <see cref="WebhookVerificationContext" /> with optional form-urlencoded parameters for providers like Twilio.</summary>
    public static WebhookVerificationContext BuildVerificationContext(HttpContext http, ReadOnlyMemory<byte> body)
    {
        var headers = http.Request.Headers.ToWebhookHeaderDictionary();
        var verification = new WebhookVerificationContext(body, headers) { RequestUrl = http.Request.GetPublicRequestUrl() };
        var contentType = http.Request.ContentType;
        if (contentType != null && contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            verification.Parameters = ParseFormUrlEncoded(body);

        return verification;
    }

    private static Dictionary<string, string> ParseFormUrlEncoded(ReadOnlyMemory<byte> body)
    {
        if (body.Length == 0)
            return new(StringComparer.OrdinalIgnoreCase);

        var s = Encoding.UTF8.GetString(body.Span);
        var parsed = QueryHelpers.ParseQuery(s);
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in parsed)
            d[kv.Key] = kv.Value.Count > 0 ? kv.Value[0]! : string.Empty;

        return d;
    }

    private IEnumerable<(string, string)> RouteTags() => new[] { (WebhookMetrics.RouteTag, _pattern) };
}