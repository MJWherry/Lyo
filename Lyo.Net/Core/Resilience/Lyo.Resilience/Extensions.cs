using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace Lyo.Resilience;

/// <summary>Extension methods for registering Lyo.Resilience with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds the default "lyo-basic" and "lyo-http" pipelines with sensible retry and timeout settings. Safe to call multiple times (idempotent).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLyoResilienceDefaults(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        if (services.Any(x => x.ServiceType == typeof(LyoResilienceDefaultsMarker)))
            return services;

        services.AddSingleton(LyoResilienceDefaultsMarker.Instance);
        services.AddResiliencePipeline(PipelineNames.Basic, (builder, context) => PipelineBuilder.ConfigureDefault(builder, context.ServiceProvider, PipelineNames.Basic));
        services.AddResiliencePipeline(PipelineNames.Http, (builder, context) => PipelineBuilder.ConfigureDefault(builder, context.ServiceProvider, PipelineNames.Http));
        return services;
    }

    /// <summary>
    /// Adds resilience pipelines loaded from a <c>Resilience</c> subsection under the given options section. Use when resilience config is nested inside your service options
    /// (e.g. TwilioOptions:Resilience).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="parentOptionsSectionName">The parent options section (e.g. "TwilioOptions").</param>
    /// <param name="resilienceSubsectionName">The subsection name under the options (default: "Resilience").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <para>For appsettings:</para>
    /// <code>
    /// "TwilioOptions": {
    ///   "AccountSid": "...",
    ///   "AuthToken": "...",
    ///   "Resilience": {
    ///     "sms-pipeline": { "Retry": { ... }, "Timeout": { ... } }
    ///   }
    /// }
    /// </code>
    /// <para>Call: AddLyoResiliencePipelinesFromOptions(services, builder.Configuration, "TwilioOptions")</para>
    /// </example>
    public static IServiceCollection AddLyoResiliencePipelinesFromOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string parentOptionsSectionName,
        string resilienceSubsectionName = "Resilience")
    {
        var sectionPath = $"{parentOptionsSectionName}:{resilienceSubsectionName}";
        return services.AddLyoResiliencePipelinesFromConfiguration(configuration, sectionPath);
    }

    /// <summary>
    /// Adds resilience pipelines loaded from configuration (e.g. appsettings.json). Pipelines are registered by name; each pipeline can define Retry, Timeout, and CircuitBreaker
    /// strategies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="configSectionName">
    /// Full config section path (default: "Lyo:ResiliencePipelines"). Use <see cref="AddLyoResiliencePipelinesFromOptions" /> when resilience is nested
    /// under service options.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLyoResiliencePipelinesFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "Lyo:ResiliencePipelines")
    {
        var section = configuration.GetSection(configSectionName);
        if (!section.Exists())
            return services;

        foreach (var pipelineSection in section.GetChildren()) {
            var pipelineName = pipelineSection.Key;
            if (string.IsNullOrWhiteSpace(pipelineName))
                continue;

            var capturedSection = pipelineSection;
            services.AddResiliencePipeline(
                pipelineName, (builder, context) => {
                    var loggerFactory = context.ServiceProvider.GetService<ILoggerFactory>();
                    var pipelineLogger = loggerFactory?.CreateLogger($"Lyo.Resilience.{pipelineName}");
                    PipelineBuilder.Configure(builder, capturedSection, context.ServiceProvider, pipelineLogger, pipelineName);
                });
        }

        return services;
    }

    /// <summary>
    /// Registers <see cref="IResilientExecutor" /> for executing actions through named resilience pipelines. Metrics (duration, success/failure) are recorded when
    /// <see cref="Lyo.Metrics.IMetrics" /> is registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddResilientExecutor(this IServiceCollection services)
    {
        services.AddLyoResilienceDefaults();
        services.AddSingleton<IResilientExecutor>(sp => {
            var pipelineProvider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
            var metrics = sp.GetService<IMetrics>();
            return new ResilientExecutor(pipelineProvider, metrics);
        });

        return services;
    }
}

internal sealed class LyoResilienceDefaultsMarker
{
    public static readonly LyoResilienceDefaultsMarker Instance = new();

    private LyoResilienceDefaultsMarker() { }
}