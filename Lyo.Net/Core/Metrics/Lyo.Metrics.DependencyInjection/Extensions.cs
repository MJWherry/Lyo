using Lyo.Exceptions;
using Lyo.Metrics.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Metrics;

/// <summary>DI helpers that register metrics services.</summary>
public static class Extensions
{
    /// <param name="services">Collection to add services to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Lyo metrics service with default options.</summary>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddLyoMetrics()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IMetrics, MetricsService>(_ => {
                var options = new MetricsOptions();
                return new(options);
            });

            return services;
        }

        /// <summary>Registers the Lyo metrics service with options built from the service provider.</summary>
        /// <param name="configure">Callback that receives the service provider and returns the options</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddLyoMetrics(Func<IServiceProvider, MetricsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
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

        /// <summary>Registers the Lyo metrics service with options filled from the service provider.</summary>
        /// <param name="configure">Callback that receives the service provider and the options to fill</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddLyoMetrics(Action<IServiceProvider, MetricsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
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

        /// <summary>Registers the Lyo metrics service with options filled by <paramref name="configure" />.</summary>
        /// <param name="configure">Callback that fills the options</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddLyoMetrics(Action<MetricsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
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

        /// <summary>Registers the Lyo metrics service from configuration.</summary>
        /// <param name="configuration">Root configuration (for example <c>builder.Configuration</c>)</param>
        /// <param name="configSectionName">Section to bind; defaults to "MetricsOptions"</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddLyoMetricsFromConfiguration(IConfiguration configuration, string configSectionName = "MetricsOptions")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddOptions<MetricsOptions>().Bind(configuration.GetSection(configSectionName)).ValidateOnStart();
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<MetricsOptions>>().Value);
            services.AddSingleton<IMetrics>(provider => {
                var options = provider.GetRequiredService<IOptions<MetricsOptions>>().Value;
                return new MetricsService(options);
            });

            return services;
        }

        /// <summary>Registers a no-op metrics service. Useful when metrics are unused but callers still need <see cref="IMetrics" />.</summary>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddNullMetrics()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IMetrics, NullMetrics>();
            return services;
        }
    }
}