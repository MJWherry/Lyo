using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Translation.Google;

/// <summary>DI helpers that register the Google Translate service.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers Google Translate by binding options from configuration.</summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">Section to bind (defaults to "GoogleTranslationOptions").</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddGoogleTranslationServiceFromConfiguration(IConfiguration configuration, string configSectionName = GoogleTranslationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(GoogleTranslationOptions))) {
                services.AddSingleton(_ => {
                    var options = LyoOptions.Bind<GoogleTranslationOptions>(configuration, configSectionName);
                    options.Validate();
                    return options;
                });
            }

            return services.RegisterGoogleTranslationService();
        }

        /// <summary>Registers Google Translate using a configuration callback.</summary>
        /// <param name="configure">Callback that receives the options object.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddGoogleTranslationService(Action<GoogleTranslationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton(_ => {
                var options = new GoogleTranslationOptions();
                configure(options);
                options.Validate();
                return options;
            });

            return services.RegisterGoogleTranslationService();
        }

        /// <summary>Registers Google Translate with a prepared options instance.</summary>
        /// <param name="options">Google translation options to use.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddGoogleTranslationService(GoogleTranslationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            return services.RegisterGoogleTranslationService();
        }

        private IServiceCollection RegisterGoogleTranslationService()
        {
            services.AddSingleton(provider => {
                var options = provider.GetRequiredService<GoogleTranslationOptions>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new GoogleTranslationService(options, provider.GetService<ILogger<GoogleTranslationService>>(), metrics, provider.GetService<HttpClient>());
            });

            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<GoogleTranslationService>());
            return services;
        }
    }
}
