using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Profanity.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Profanity;

/// <summary>DI helpers that register profanity filter service with dependency injection.</summary>
public static class Extensions
{
    private static void AddFileProfanityFilterService(IServiceCollection services)
    {
        services.AddSingleton<FileProfanityFilterService>(provider => {
            var options = provider.GetRequiredService<FileProfanityFilterOptions>();
            var logger = provider.GetService<ILogger<FileProfanityFilterService>>();
            var metrics = provider.GetService<IMetrics>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, logger, metrics, httpClient);
        });

        services.AddSingleton<IProfanityFilterService>(provider => provider.GetRequiredService<FileProfanityFilterService>());
    }

    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers file-based profanity filter service with default options.</summary>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddProfanityFilterService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new FileProfanityFilterOptions();
            options.Validate();
            services.AddSingleton(options);
            AddFileProfanityFilterService(services);
            return services;
        }

        /// <summary>Registers file-based profanity filter service with options configuration callback.</summary>
        /// <param name="configure">Callback that fills the options object.</param>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddProfanityFilterService(Action<FileProfanityFilterOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new FileProfanityFilterOptions();
            configure(options);
            options.Validate();
            services.AddSingleton(options);

            AddFileProfanityFilterService(services);
            return services;
        }

        /// <summary>Registers file-based profanity filter service with options bound from configuration.</summary>
        /// <param name="configuration">Host configuration root.</param>
        /// <param name="configSectionName">Configuration section name. Default: "ProfanityFilter".</param>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddProfanityFilterServiceFromConfiguration(IConfiguration configuration, string configSectionName = FileProfanityFilterOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new FileProfanityFilterOptions();
            configuration.GetSection(configSectionName).Bind(options);
            options.Validate();
            services.AddSingleton(options);

            AddFileProfanityFilterService(services);
            return services;
        }
    }
}