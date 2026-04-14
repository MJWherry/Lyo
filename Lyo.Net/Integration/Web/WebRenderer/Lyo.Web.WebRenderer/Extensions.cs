using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.WebRenderer;

/// <summary>Extension methods for registering WebRendererService.</summary>
public static class Extensions
{
    /// <summary>Adds WebRendererService using configuration binding. Binds WebRenderOptions from the specified config section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
    /// <param name="configSectionName">The configuration section name (defaults to "WebRenderOptions").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebRendererServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = WebRenderOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        if (!services.Any(s => s.ServiceType == typeof(WebRenderOptions))) {
            services.AddSingleton<WebRenderOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new WebRenderOptions();
                if (section.Exists())
                    section.Bind(options);

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