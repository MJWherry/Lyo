using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.WebRenderer;

/// <summary>DI helpers for registering WebRendererService.</summary>
public static class Extensions
{
    /// <summary>Adds WebRendererService by binding WebRenderOptions from the given config section.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration root (for example builder.Configuration).</param>
    /// <param name="configSectionName">Configuration section name. Defaults to "WebRenderOptions".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebRendererServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = WebRenderOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        if (!services.Any(s => s.ServiceType == typeof(WebRenderOptions))) {
            services.AddSingleton<WebRenderOptions>(_ => {
                var options = LyoOptions.Bind<WebRenderOptions>(configuration, configSectionName);

                return options;
            });
        }

        services.AddScoped<HtmlRenderer>(provider => new(provider, provider.GetRequiredService<ILoggerFactory>()));
        services.AddScoped<IWebRendererService>(provider => new WebRendererService(
            provider.GetRequiredService<HtmlRenderer>(), provider.GetService<ILogger<WebRendererService>>(), provider.GetService<IMetrics>(),
            provider.GetService<WebRenderOptions>()));

        return services;
    }
}