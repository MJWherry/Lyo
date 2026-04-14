using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Profanity.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Profanity;

/// <summary>Extension methods for registering profanity filter service with dependency injection.</summary>
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

    /// <summary>Adds file-based profanity filter service with default options.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProfanityFilterService(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddSingleton<FileProfanityFilterOptions>(_ => new());
        AddFileProfanityFilterService(services);
        return services;
    }

    /// <summary>Adds file-based profanity filter service with options configuration callback.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProfanityFilterService(this IServiceCollection services, Action<FileProfanityFilterOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton<FileProfanityFilterOptions>(_ => {
            var options = new FileProfanityFilterOptions();
            configure(options);
            return options;
        });

        AddFileProfanityFilterService(services);
        return services;
    }

    /// <summary>Adds file-based profanity filter service with options bound from configuration.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configSectionName">The configuration section name. Default: "ProfanityFilter".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProfanityFilterServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = FileProfanityFilterOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        services.AddSingleton<FileProfanityFilterOptions>(_ => {
            var options = new FileProfanityFilterOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return options;
        });

        AddFileProfanityFilterService(services);
        return services;
    }
}