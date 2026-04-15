using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Endato.Client;

/// <summary>Extension methods for registering Endato client with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds Endato client to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configSectionName">The configuration section name (defaults to "EndatoClient").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>Example configuration in appsettings.json:</para>
    /// <code>
    /// {
    ///   "EndatoClient": {
    ///     "BaseUrl": "https://api.endato.com",
    ///     "ApName": "your-ap-name",
    ///     "ApPassword": "your-ap-password"
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddEndatoClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = EndatoClientOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

        // Configure EndatoClientOptions from configuration (if not already registered)
        if (!services.Any(s => s.ServiceType == typeof(EndatoClientOptions))) {
            services.AddSingleton<EndatoClientOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new EndatoClientOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        // Register the client
        services.AddSingleton<EndatoClient>(provider => {
            var options = provider.GetRequiredService<EndatoClientOptions>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }

    /// <summary>Adds Endato client to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action that receives the config object to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEndatoClient(this IServiceCollection services, Action<EndatoClientOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton<EndatoClientOptions>(_ => {
            var options = new EndatoClientOptions();
            configure(options);
            return options;
        });

        services.AddSingleton<EndatoClient>(provider => {
            var options = provider.GetRequiredService<EndatoClientOptions>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }

    /// <summary>Adds Endato client to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Endato client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEndatoClient(this IServiceCollection services, EndatoClientOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddSingleton<EndatoClient>(provider => {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }
}