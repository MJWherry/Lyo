using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Translation.Google;

/// <summary>Extension methods for registering Google Translate service with dependency injection.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds Google Translate service to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration root.</param>
        /// <param name="configSectionName">The configuration section name (defaults to "GoogleTranslationOptions").</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddGoogleTranslationServiceFromConfiguration(IConfiguration configuration, string configSectionName = GoogleTranslationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            // Configure GoogleTranslationOptions from configuration (if not already registered)
            if (!services.Any(s => s.ServiceType == typeof(GoogleTranslationOptions))) {
                services.AddSingleton<GoogleTranslationOptions>(_ => {
                    var section = configuration.GetSection(configSectionName);
                    var options = new GoogleTranslationOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }

            // Register the translation service
            services.AddSingleton<GoogleTranslationService>(provider => {
                var options = provider.GetRequiredService<GoogleTranslationOptions>();
                var logger = provider.GetService<ILogger<GoogleTranslationService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var httpClient = provider.GetService<HttpClient>();
                return new(options, logger, metrics, httpClient);
            });

            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<GoogleTranslationService>());
            return services;
        }

        /// <summary>Adds Google Translate service to the service collection.</summary>
        /// <param name="configure">Action that receives the config object to configure.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddGoogleTranslationService(Action<GoogleTranslationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<GoogleTranslationOptions>(_ => {
                var options = new GoogleTranslationOptions();
                configure(options);
                return options;
            });

            services.AddSingleton<GoogleTranslationService>(provider => {
                var options = provider.GetRequiredService<GoogleTranslationOptions>();
                var logger = provider.GetService<ILogger<GoogleTranslationService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var httpClient = provider.GetService<HttpClient>();
                return new(options, logger, metrics, httpClient);
            });

            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<GoogleTranslationService>());
            return services;
        }

        /// <summary>Adds Google Translate service to the service collection.</summary>
        /// <param name="options">The Google translation options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddGoogleTranslationService(GoogleTranslationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<GoogleTranslationService>(provider => {
                var logger = provider.GetService<ILogger<GoogleTranslationService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var httpClient = provider.GetService<HttpClient>();
                return new(options, logger, metrics, httpClient);
            });

            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<GoogleTranslationService>());
            return services;
        }
    }
}