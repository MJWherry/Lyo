using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Scraping;

/// <summary>Extension methods for Scraper registration.</summary>
public static class Extensions
{
    /// <summary>Adds the Scraper to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScraper(this IServiceCollection services, Action<ScraperOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        var options = new ScraperOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddScoped<Scraper>(sp => {
            var opts = sp.GetRequiredService<ScraperOptions>();
            var executor = sp.GetRequiredService<IResilientExecutor>();
            var logger = sp.GetService<ILogger<Scraper>>();
            var metrics = sp.GetService<IMetrics>();
            return new(opts, executor, logger, metrics);
        });

        return services;
    }

    /// <summary>Adds the Scraper to the service collection with explicit options.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Scraper options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScraper(this IServiceCollection services, ScraperOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddScoped<Scraper>(sp => {
            var opts = sp.GetRequiredService<ScraperOptions>();
            var executor = sp.GetRequiredService<IResilientExecutor>();
            var logger = sp.GetService<ILogger<Scraper>>();
            var metrics = sp.GetService<IMetrics>();
            return new(opts, executor, logger, metrics);
        });

        return services;
    }
}