using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;

namespace Lyo.Resilience;

/// <summary>Helpers that attach Lyo.Resilience to HttpClient.</summary>
public static class HttpExtensions
{
    /// <summary>Adds a resilience handler that uses the default <see cref="PipelineNames.Http" /> pipeline. Registers that pipeline when it is not already present.</summary>
    /// <remarks>
    /// Keep resilience on the HttpClient. Do not wrap HttpClient-using code with <see cref="IResilientExecutor" /> when this handler is in place — nested resilience produces
    /// exponential retries.
    /// </remarks>
    /// <param name="builder">HTTP client builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHttpClientBuilder AddLyoResilienceHandler(this IHttpClientBuilder builder)
    {
        builder.Services.AddLyoResilienceDefaults();
        return builder.AddLyoResilienceHandler(PipelineNames.Http);
    }

    /// <summary>Adds a resilience handler that wraps each request in the named pipeline (retry, timeout, and circuit breaker).</summary>
    /// <remarks>
    /// Keep resilience on the HttpClient. Do not wrap HttpClient-using code with <see cref="IResilientExecutor" /> when this handler is in place — nested resilience produces
    /// exponential retries.
    /// </remarks>
    /// <param name="builder">HTTP client builder.</param>
    /// <param name="pipelineName">Pipeline name (must already be registered via AddLyoResiliencePipelinesFromConfiguration or AddLyoResilienceDefaults).</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHttpClientBuilder AddLyoResilienceHandler(this IHttpClientBuilder builder, string pipelineName)
    {
        ArgumentHelpers.ThrowIfNull(builder);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pipelineName);
        builder.AddHttpMessageHandler(services => {
            var pipelineProvider = services.GetRequiredService<ResiliencePipelineProvider<string>>();
            var metrics = services.GetService<IMetrics>() ?? NullMetrics.Instance;
            return new ResilienceHttpHandler(pipelineProvider, pipelineName, metrics);
        });

        return builder;
    }
}