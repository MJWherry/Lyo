using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace Lyo.Resilience;

/// <summary>Helpers for registering Lyo.Resilience with dependency injection.</summary>
public static class Extensions
{
    /// <param name="services">Collection to add services to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the default "lyo-basic" and "lyo-http" pipelines with retry and timeout defaults. Safe to call more than once (idempotent).</summary>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddLyoResilienceDefaults()
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (services.Any(x => x.ServiceType == typeof(LyoResilienceDefaultsMarker)))
                return services;

            services.AddSingleton(LyoResilienceDefaultsMarker.Instance);
            services.AddResiliencePipeline(PipelineNames.Basic, (builder, context) => PipelineBuilder.ConfigureDefault(builder, context.ServiceProvider, PipelineNames.Basic));
            services.AddResiliencePipeline(PipelineNames.Http, (builder, context) => PipelineBuilder.ConfigureDefault(builder, context.ServiceProvider, PipelineNames.Http));
            return services;
        }

        /// <summary>
        /// Registers resilience pipelines from a <c>Resilience</c> subsection under the given options section. Use when resilience config is nested inside service options
        /// (for example TwilioOptions:Resilience).
        /// </summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="parentOptionsSectionName">Parent options section (for example "TwilioOptions").</param>
        /// <param name="resilienceSubsectionName">Subsection name under those options (default: "Resilience").</param>
        /// <returns>The same collection, for chaining</returns>
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
        public IServiceCollection AddLyoResiliencePipelinesFromOptions(
            IConfiguration configuration,
            string parentOptionsSectionName,
            string resilienceSubsectionName = "Resilience")
        {
            var sectionPath = $"{parentOptionsSectionName}:{resilienceSubsectionName}";
            return services.AddLyoResiliencePipelinesFromConfiguration(configuration, sectionPath);
        }

        /// <summary>
        /// Registers resilience pipelines from configuration (for example appsettings.json). Each named pipeline may define Retry, Timeout, and CircuitBreaker
        /// strategies.
        /// </summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">
        /// Full config section path (default: "Lyo:ResiliencePipelines"). Use <see cref="AddLyoResiliencePipelinesFromOptions" /> when resilience is nested
        /// under service options.
        /// </param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddLyoResiliencePipelinesFromConfiguration(IConfiguration configuration, string configSectionName = "Lyo:ResiliencePipelines")
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
        /// Registers <see cref="IResilientExecutor" /> so callers can run work through named resilience pipelines. Duration and success/failure metrics are recorded when
        /// <see cref="Lyo.Metrics.IMetrics" /> is registered.
        /// </summary>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddResilientExecutor()
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
}

internal sealed class LyoResilienceDefaultsMarker
{
    public static readonly LyoResilienceDefaultsMarker Instance = new();

    private LyoResilienceDefaultsMarker() { }
}