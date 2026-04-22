using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation.Playwright.Browser;
using Lyo.Web.Automation.Playwright.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Playwright.Service;

/// <summary>Extension methods for Playwright browser automation registration.</summary>
public static class PlaywrightServiceExtensions
{
    /// <summary>Registers <see cref="PlaywrightBrowserOptions" /> and a scoped <see cref="PlaywrightBrowser" /> for direct injection.</summary>
    public static IServiceCollection AddPlaywrightBrowser(this IServiceCollection services, Action<PlaywrightBrowserOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        RegisterOptionsAndBrowser(services, configure);
        return services;
    }

    /// <summary>
    /// Registers <see cref="PlaywrightBrowserOptions" />, scoped <see cref="PlaywrightBrowser" />, and singleton <see cref="IPlaywrightBrowserService" /> for session-based
    /// usage.
    /// </summary>
    public static IServiceCollection AddPlaywrightBrowserService(this IServiceCollection services, Action<PlaywrightBrowserOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        RegisterOptionsAndBrowser(services, configure);
        RegisterPlaywrightBrowserServiceSingleton(services);
        return services;
    }

    /// <summary>Binds <paramref name="configuration" /> to <see cref="PlaywrightBrowserOptions" /> and registers Playwright services.</summary>
    public static IServiceCollection AddPlaywrightBrowserServiceFromConfiguration(this IServiceCollection services, IConfiguration configuration, string? configSectionName = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        var sectionName = string.IsNullOrWhiteSpace(configSectionName) ? PlaywrightBrowserOptions.SectionName : configSectionName!;
        services.AddSingleton(_ => {
            var o = new PlaywrightBrowserOptions();
            configuration.GetSection(sectionName).Bind(o);
            return o;
        });

        services.AddScoped(RegisterPlaywrightBrowser);
        RegisterPlaywrightBrowserServiceSingleton(services);
        return services;
    }

    /// <summary>Registers explicit options plus <see cref="IPlaywrightBrowserService" />.</summary>
    public static IServiceCollection AddPlaywrightBrowserService(this IServiceCollection services, PlaywrightBrowserOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddScoped(RegisterPlaywrightBrowser);
        RegisterPlaywrightBrowserServiceSingleton(services);
        return services;
    }

    /// <summary>Registers explicit options and a scoped <see cref="PlaywrightBrowser" />.</summary>
    public static IServiceCollection AddPlaywrightBrowser(this IServiceCollection services, PlaywrightBrowserOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddScoped(RegisterPlaywrightBrowser);
        return services;
    }

    private static void RegisterOptionsAndBrowser(IServiceCollection services, Action<PlaywrightBrowserOptions>? configure)
    {
        services.AddSingleton(_ => {
            var options = new PlaywrightBrowserOptions();
            configure?.Invoke(options);
            return options;
        });

        services.AddScoped(RegisterPlaywrightBrowser);
    }

    private static void RegisterPlaywrightBrowserServiceSingleton(IServiceCollection services)
        => services.AddSingleton<IPlaywrightBrowserService>(sp => new PlaywrightBrowserService(
            sp.GetRequiredService<PlaywrightBrowserOptions>(), sp.GetService<ILoggerFactory>(), sp.GetService<IMetrics>()));

    private static PlaywrightBrowser RegisterPlaywrightBrowser(IServiceProvider sp)
    {
        var opts = sp.GetRequiredService<PlaywrightBrowserOptions>();
        var logger = sp.GetService<ILogger<PlaywrightBrowser>>();
        var metrics = sp.GetService<IMetrics>();
        return new(opts, null, logger, metrics);
    }
}