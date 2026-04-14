using Lyo.Exceptions;
using Lyo.ShortUrl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.ShortUrl;

/// <summary>Extension methods for URL shortener service registration.</summary>
public static class Extensions
{
    /// <summary>Adds a URL shortener service to the service collection.</summary>
    /// <typeparam name="TService">The URL shortener service implementation type.</typeparam>
    /// <typeparam name="TOptions">The URL shortener service options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddShortUrlService<TService, TOptions>(this IServiceCollection services, Action<TOptions>? configure = null)
        where TService : class, IShortUrlService where TOptions : ShortUrlServiceOptions, new()
    {
        if (configure != null)
            services.Configure(configure);

        services.AddSingleton<IShortUrlService, TService>();
        return services;
    }

    /// <summary>Adds a URL shortener service to the service collection with explicit options.</summary>
    /// <typeparam name="TService">The URL shortener service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The URL shortener service options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddShortUrlService<TService>(this IServiceCollection services, ShortUrlServiceOptions options)
        where TService : class, IShortUrlService
    {
        services.AddSingleton(options);
        services.AddSingleton<IShortUrlService, TService>();
        return services;
    }

    /// <summary>Adds short URL generator service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddShortUrlGenerator(this IServiceCollection services)
    {
        services.AddSingleton<IShortUrlGenerator, ShortUrlGenerator>();
        return services;
    }

    /// <summary>Adds URL shortener service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddShortUrl(this IServiceCollection services, Action<ShortUrlServiceOptions>? configure = null)
    {
        var options = new ShortUrlServiceOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddShortUrlGenerator();
        services.AddSingleton<IShortUrlService, ShortUrlService>();
        return services;
    }

    /// <summary>Adds URL shortener service to the service collection with explicit options.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The URL shortener service options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddShortUrl(this IServiceCollection services, ShortUrlServiceOptions options)
    {
        services.AddSingleton(options);
        services.AddShortUrlGenerator();
        services.AddSingleton<IShortUrlService, ShortUrlService>();
        return services;
    }

    /// <summary>Adds URL shortener service to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configSectionName">The configuration section name (defaults to "ShortUrlOptions"). The section will be bound from IConfiguration if available.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>This method binds configuration from IConfiguration if it's registered in the service collection. If IConfiguration is not available, the options will use default values.</para>
    /// <para>Example configuration in appsettings.json:</para>
    /// <code>
    /// {
    ///   "ShortUrlOptions": {
    ///     "BaseUrl": "https://short.ly",
    ///     "DefaultExpirationDays": 30,
    ///     "MaxAliasLength": 50,
    ///     "MinAliasLength": 3,
    ///     "AllowCustomAliases": true,
    ///     "EnableMetrics": false,
    ///     "EnforceHttps": false
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddShortUrlFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = ShortUrlServiceOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        if (!services.Any(s => s.ServiceType == typeof(ShortUrlServiceOptions))) {
            services.AddSingleton<ShortUrlServiceOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new ShortUrlServiceOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        services.AddShortUrlGenerator();
        services.AddSingleton<IShortUrlService, ShortUrlService>();
        return services;
    }
}