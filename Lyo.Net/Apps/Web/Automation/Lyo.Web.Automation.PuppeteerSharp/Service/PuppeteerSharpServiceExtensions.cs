using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation.PuppeteerSharp.Browser;
using Lyo.Web.Automation.PuppeteerSharp.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.PuppeteerSharp.Service;

/// <summary>Extension methods for PuppeteerSharp browser automation registration.</summary>
public static class PuppeteerSharpServiceExtensions
{
    /// <summary>Adds <see cref="PuppeteerSharpBrowserOptions" /> and a scoped <see cref="PuppeteerSharpBrowser" />.</summary>
    public static IServiceCollection AddPuppeteerSharpBrowser(this IServiceCollection services, Action<PuppeteerSharpBrowserOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        RegisterOptionsAndBrowser(services, configure);
        return services;
    }

    /// <summary>Adds options, a scoped browser, and singleton <see cref="IPuppeteerSharpBrowserService" />.</summary>
    public static IServiceCollection AddPuppeteerSharpBrowserService(this IServiceCollection services, Action<PuppeteerSharpBrowserOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        RegisterOptionsAndBrowser(services, configure);
        RegisterServiceSingleton(services);
        return services;
    }

    /// <summary>Binds <paramref name="configuration" /> to <see cref="PuppeteerSharpBrowserOptions" /> and registers the browser service.</summary>
    public static IServiceCollection AddPuppeteerSharpBrowserServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string? configSectionName = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        var sectionName = configSectionName.OrDefaultIfWhiteSpace(PuppeteerSharpBrowserOptions.SectionName);
        services.AddSingleton(_ => {
            var options = new PuppeteerSharpBrowserOptions();
            configuration.GetSection(sectionName).Bind(options);
            options.Validate();
            return options;
        });

        services.AddScoped(RegisterBrowser);
        RegisterServiceSingleton(services);
        return services;
    }

    /// <summary>Adds explicit options plus <see cref="IPuppeteerSharpBrowserService" />.</summary>
    public static IServiceCollection AddPuppeteerSharpBrowserService(this IServiceCollection services, PuppeteerSharpBrowserOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        services.AddSingleton(options);
        services.AddScoped(RegisterBrowser);
        RegisterServiceSingleton(services);
        return services;
    }

    /// <summary>Adds explicit options and a scoped <see cref="PuppeteerSharpBrowser" />.</summary>
    public static IServiceCollection AddPuppeteerSharpBrowser(this IServiceCollection services, PuppeteerSharpBrowserOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        services.AddSingleton(options);
        services.AddScoped(RegisterBrowser);
        return services;
    }

    private static void RegisterOptionsAndBrowser(IServiceCollection services, Action<PuppeteerSharpBrowserOptions>? configure)
    {
        services.AddSingleton(_ => {
            var options = new PuppeteerSharpBrowserOptions();
            configure?.Invoke(options);
            options.Validate();
            return options;
        });

        services.AddScoped(RegisterBrowser);
    }

    private static void RegisterServiceSingleton(IServiceCollection services)
        => services.AddSingleton<IPuppeteerSharpBrowserService>(sp => new PuppeteerSharpBrowserService(
            sp.GetRequiredService<PuppeteerSharpBrowserOptions>(), sp.GetService<ILoggerFactory>(), sp.GetService<IMetrics>()));

    private static PuppeteerSharpBrowser RegisterBrowser(IServiceProvider sp)
    {
        var opts = sp.GetRequiredService<PuppeteerSharpBrowserOptions>().Clone();
        var logger = sp.GetService<ILogger<PuppeteerSharpBrowser>>();
        var metrics = sp.GetService<IMetrics>();
        return new(opts, null, logger, metrics);
    }
}