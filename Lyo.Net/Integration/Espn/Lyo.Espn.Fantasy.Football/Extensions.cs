using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Espn.Fantasy.Football;

/// <summary>Extension methods for registering <see cref="FantasyFootballClient" /> with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds the fantasy football client using configuration binding.</summary>
    public static IServiceCollection AddFantasyFootballClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = FantasyFootballClientOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        if (!services.Any(i => i.ServiceType == typeof(FantasyFootballClientOptions))) {
            services.AddSingleton<FantasyFootballClientOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new FantasyFootballClientOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        services.AddSingleton<FantasyFootballClient>(provider => {
            var options = provider.GetRequiredService<FantasyFootballClientOptions>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }

    /// <summary>Adds the fantasy football client with inline configuration.</summary>
    public static IServiceCollection AddFantasyFootballClient(this IServiceCollection services, Action<FantasyFootballClientOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton<FantasyFootballClientOptions>(_ => {
            var options = new FantasyFootballClientOptions();
            configure(options);
            return options;
        });

        services.AddSingleton<FantasyFootballClient>(provider => {
            var options = provider.GetRequiredService<FantasyFootballClientOptions>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }

    /// <summary>Adds the fantasy football client with a pre-built options instance.</summary>
    public static IServiceCollection AddFantasyFootballClient(this IServiceCollection services, FantasyFootballClientOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddSingleton<FantasyFootballClient>(provider => {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }
}