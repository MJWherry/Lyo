using Lyo.Exceptions;
using Lyo.Metrics.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Metrics;

/// <summary>Extension methods for registering metrics services with dependency injection.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds Lyo metrics service to the service collection with default options.</summary>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLyoMetrics()
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            services.AddSingleton<IMetrics, MetricsService>(_ => {
                var options = new MetricsOptions();
                return new(options);
            });

            return services;
        }

        /// <summary>Adds Lyo metrics service to the service collection with custom options.</summary>
        /// <param name="configure">Function that receives the service provider and returns the configured options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLyoMetrics(Func<IServiceProvider, MetricsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<MetricsOptions>(provider => {
                var options = configure(provider);
                return options;
            });

            services.AddSingleton<IMetrics>(provider => {
                var options = provider.GetRequiredService<MetricsOptions>();
                return new MetricsService(options);
            });

            return services;
        }

        /// <summary>Adds Lyo metrics service to the service collection with custom options.</summary>
        /// <param name="configure">Action that receives the service provider and config object to configure</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLyoMetrics(Action<IServiceProvider, MetricsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<MetricsOptions>(provider => {
                var options = new MetricsOptions();
                configure(provider, options);
                return options;
            });

            services.AddSingleton<IMetrics>(provider => {
                var options = provider.GetRequiredService<MetricsOptions>();
                return new MetricsService(options);
            });

            return services;
        }

        /// <summary>Adds Lyo metrics service to the service collection with custom options.</summary>
        /// <param name="configure">Action that receives the config object to configure</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLyoMetrics(Action<MetricsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<MetricsOptions>(_ => {
                var options = new MetricsOptions();
                configure(options);
                return options;
            });

            services.AddSingleton<IMetrics>(provider => {
                var options = provider.GetRequiredService<MetricsOptions>();
                return new MetricsService(options);
            });

            return services;
        }

        /// <summary>Adds Lyo metrics service to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">The configuration section name (defaults to "MetricsOptions")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLyoMetricsFromConfiguration(IConfiguration configuration, string configSectionName = "MetricsOptions")
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
            services.AddOptions<MetricsOptions>().Bind(configuration.GetSection(configSectionName)).ValidateOnStart();
            services.AddSingleton<IMetrics>(provider => {
                var options = provider.GetRequiredService<IOptions<MetricsOptions>>().Value;
                return new MetricsService(options);
            });

            return services;
        }

        /// <summary>Adds a null metrics service (no-op) to the service collection. Useful when metrics are not needed but services require IMetrics.</summary>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddNullMetrics()
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            services.AddSingleton<IMetrics, NullMetrics>();
            return services;
        }
    }
}