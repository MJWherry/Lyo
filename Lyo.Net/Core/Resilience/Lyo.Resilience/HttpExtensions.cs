using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;

namespace Lyo.Resilience;

/// <summary>Extension methods for adding Lyo.Resilience to HttpClient.</summary>
public static class HttpExtensions
{
    /// <summary>Adds a resilience handler using the default <see cref="PipelineNames.Http" /> pipeline. Registers the default pipeline if not already present.</summary>
    /// <remarks>
    /// Resilience belongs on the HttpClient. Do NOT wrap HttpClient-using code with <see cref="IResilientExecutor" /> when using this handler—that causes nested resilience and
    /// exponential retries.
    /// </remarks>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddLyoResilienceHandler(this IHttpClientBuilder builder)
    {
        builder.Services.AddLyoResilienceDefaults();
        return builder.AddLyoResilienceHandler(PipelineNames.Http);
    }

    /// <summary>Adds a resilience handler that wraps HTTP requests with the specified named pipeline. The pipeline applies retry, timeout, and circuit breaker to each request.</summary>
    /// <remarks>
    /// Resilience belongs on the HttpClient. Do NOT wrap HttpClient-using code with <see cref="IResilientExecutor" /> when using this handler—that causes nested resilience and
    /// exponential retries.
    /// </remarks>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="pipelineName">The name of the resilience pipeline (must be registered via AddLyoResiliencePipelinesFromConfiguration or AddLyoResilienceDefaults).</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddLyoResilienceHandler(this IHttpClientBuilder builder, string pipelineName)
    {
        ArgumentHelpers.ThrowIfNull(builder, nameof(builder));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pipelineName, nameof(pipelineName));
        builder.AddHttpMessageHandler(services => {
            var pipelineProvider = services.GetRequiredService<ResiliencePipelineProvider<string>>();
            var metrics = services.GetService<IMetrics>() ?? NullMetrics.Instance;
            return new ResilienceHttpHandler(pipelineProvider, pipelineName, metrics);
        });

        return builder;
    }
}