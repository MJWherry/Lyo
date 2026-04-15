using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Typecast.Client;

/// <summary>Extension methods for registering Typecast client with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds Typecast client to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
    /// <param name="configSectionName">The configuration section name (defaults to "TypecastClient").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>Example configuration in appsettings.json:</para>
    /// <code>
    /// {
    ///   "TypecastClient": {
    ///     "ApiKey": "your-api-key",
    ///     "BaseUrl": "https://api.typecast.ai"
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddTypecastClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = TypecastClientOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

        // Configure TypecastClientOptions from configuration (if not already registered)
        if (!services.Any(s => s.ServiceType == typeof(TypecastClientOptions))) {
            services.AddSingleton<TypecastClientOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new TypecastClientOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        // Register the client
        services.AddSingleton<TypecastClient>(provider => {
            var options = provider.GetRequiredService<TypecastClientOptions>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }

    /// <summary>Adds Typecast client to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action that receives the config object to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTypecastClient(this IServiceCollection services, Action<TypecastClientOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton<TypecastClientOptions>(_ => {
            var options = new TypecastClientOptions();
            configure(options);
            return options;
        });

        services.AddSingleton<TypecastClient>(provider => {
            var options = provider.GetRequiredService<TypecastClientOptions>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }

    /// <summary>Adds Typecast client to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Typecast client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTypecastClient(this IServiceCollection services, TypecastClientOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddSingleton<TypecastClient>(provider => {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory, httpClient);
        });

        return services;
    }
}