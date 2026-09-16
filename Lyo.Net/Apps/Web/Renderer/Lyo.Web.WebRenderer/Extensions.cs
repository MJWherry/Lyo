using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.WebRenderer.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.WebRenderer;

/// <summary>DI helpers for registering <see cref="WebRendererService" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds the web renderer with options set by <paramref name="configure" />.</summary>
        /// <param name="configure">Callback that mutates <see cref="WebRenderOptions" /> before validation.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddWebRendererService(Action<WebRenderOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new WebRenderOptions();
            configure(options);
            return services.AddWebRendererService(options);
        }

        /// <summary>Adds the web renderer by binding <see cref="WebRenderOptions" /> from configuration.</summary>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Configuration section name. Defaults to <see cref="WebRenderOptions.SectionName" />.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddWebRendererServiceFromConfiguration(IConfiguration configuration, string configSectionName = WebRenderOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new WebRenderOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddWebRendererService(options);
        }

        /// <summary>
        /// Adds a scoped <see cref="HtmlRenderer" /> and <see cref="IWebRendererService" />. An existing
        /// <see cref="WebRenderOptions" /> registration is kept. <see cref="ILogger{TCategoryName}" /> must be registered.
        /// </summary>
        /// <param name="options">Validated options. Used when the collection does not already contain <see cref="WebRenderOptions" />.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddWebRendererService(WebRenderOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(options);
            services.TryAddSingleton<BrowserSession>(provider => {
                var registered = provider.GetRequiredService<WebRenderOptions>();
                var recorded = registered.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new BrowserSession(registered, provider.GetService<ILogger<BrowserSession>>(), recorded);
            });
            services.TryAddScoped<HtmlRenderer>(provider => new(provider, provider.GetRequiredService<ILoggerFactory>()));
            services.TryAddScoped<WebRendererService>(provider => new WebRendererService(
                provider.GetRequiredService<HtmlRenderer>(),
                provider.GetRequiredService<ILogger<WebRendererService>>(),
                provider.GetService<IMetrics>(),
                provider.GetService<WebRenderOptions>(),
                provider.GetRequiredService<BrowserSession>()));
            services.TryAddScoped<IWebRendererService>(provider => provider.GetRequiredService<WebRendererService>());
            services.TryAddScoped<IWebRenderGenerator>(provider => provider.GetRequiredService<WebRendererService>());
            return services;
        }
    }
}
