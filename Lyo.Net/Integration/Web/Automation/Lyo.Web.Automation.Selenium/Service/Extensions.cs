using Lyo.Exceptions;
using Lyo.IO.Temp;
using Lyo.Metrics;
using Lyo.Web.Automation;
using Lyo.Web.Automation.Selenium.Browser;
using Lyo.Web.Automation.Selenium.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Selenium.Service;

/// <summary>Extension methods for Selenium browser automation registration.</summary>
public static class Extensions
{
    /// <summary>Registers <see cref="SeleniumBrowserOptions" /> and a scoped <see cref="LyoBrowser" /> for direct injection (legacy style).</summary>
    public static IServiceCollection AddSeleniumBrowser(this IServiceCollection services, Action<SeleniumBrowserOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        RegisterOptionsAndBrowser(services, configure);
        return services;
    }

    /// <summary>Registers options built with <see cref="SeleniumBrowserOptionsBuilder" /> and a scoped <see cref="LyoBrowser" />.</summary>
    public static IServiceCollection AddSeleniumBrowser(this IServiceCollection services, Action<SeleniumBrowserOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton(_ => CreateOptionsFromBuilder(configure));
        services.AddScoped(RegisterSeleniumBrowser);
        return services;
    }

    /// <summary>
    /// Registers <see cref="SeleniumBrowserOptions" />, scoped <see cref="LyoBrowser" />, and singleton <see cref="ISeleniumBrowserService" /> for session-based usage (see <see cref="ISeleniumBrowserService.CreateSession" />).
    /// </summary>
    public static IServiceCollection AddSeleniumBrowserService(this IServiceCollection services, Action<SeleniumBrowserOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        RegisterOptionsAndBrowser(services, configure);
        RegisterSeleniumBrowserServiceSingleton(services);
        return services;
    }

    /// <summary>Registers options from a fluent builder plus <see cref="ISeleniumBrowserService" />.</summary>
    public static IServiceCollection AddSeleniumBrowserService(this IServiceCollection services, Action<SeleniumBrowserOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton(_ => CreateOptionsFromBuilder(configure));
        services.AddScoped(RegisterSeleniumBrowser);
        RegisterSeleniumBrowserServiceSingleton(services);
        return services;
    }

    /// <summary>
    /// Binds <paramref name="configuration" /> to a singleton <see cref="SeleniumBrowserOptions" /> and registers browser services (no <c>IOptions&lt;T&gt;</c>).
    /// </summary>
    /// <param name="configSectionName">Defaults to <see cref="SeleniumBrowserOptions.SectionName" />.</param>
    public static IServiceCollection AddSeleniumBrowserServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string? configSectionName = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        var sectionName = string.IsNullOrWhiteSpace(configSectionName) ? SeleniumBrowserOptions.SectionName : configSectionName!;
        services.AddSingleton(_ => {
            var o = new SeleniumBrowserOptions();
            ConfigurationBinder.Bind(configuration.GetSection(sectionName), o);
            return o;
        });
        services.AddScoped(RegisterSeleniumBrowser);
        RegisterSeleniumBrowserServiceSingleton(services);
        return services;
    }

    /// <summary>Registers scraper with explicit options (scoped <see cref="LyoBrowser" /> per request).</summary>
    public static IServiceCollection AddSeleniumBrowser(this IServiceCollection services, SeleniumBrowserOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddScoped(RegisterSeleniumBrowser);
        return services;
    }

    /// <summary>Registers explicit options plus <see cref="ISeleniumBrowserService" />.</summary>
    public static IServiceCollection AddSeleniumBrowserService(this IServiceCollection services, SeleniumBrowserOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddScoped(RegisterSeleniumBrowser);
        RegisterSeleniumBrowserServiceSingleton(services);
        return services;
    }

    private static void RegisterOptionsAndBrowser(IServiceCollection services, Action<SeleniumBrowserOptions>? configure)
    {
        services.AddSingleton(_ => {
            var options = new SeleniumBrowserOptions();
            configure?.Invoke(options);
            return options;
        });
        services.AddScoped(RegisterSeleniumBrowser);
    }

    private static void RegisterSeleniumBrowserServiceSingleton(IServiceCollection services)
    {
        services.AddSingleton<ISeleniumBrowserService>(sp => new SeleniumBrowserService(
            sp.GetRequiredService<SeleniumBrowserOptions>(),
            sp.GetService<IIOTempService>(),
            sp.GetService<ILoggerFactory>(),
            sp.GetService<IMetrics>()));
    }

    private static SeleniumBrowserOptions CreateOptionsFromBuilder(Action<SeleniumBrowserOptionsBuilder> configure)
    {
        var b = SeleniumBrowserOptionsBuilder.New();
        configure(b);
        return b.Build();
    }

    private static LyoBrowser RegisterSeleniumBrowser(IServiceProvider sp)
    {
        var opts = sp.GetRequiredService<SeleniumBrowserOptions>();
        var logger = sp.GetService<ILogger<LyoBrowser>>();
        var metrics = sp.GetService<IMetrics>();
        return new(opts, logger, metrics);
    }
}
