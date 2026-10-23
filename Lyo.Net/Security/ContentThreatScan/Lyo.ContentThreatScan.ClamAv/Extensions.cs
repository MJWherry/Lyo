using Lyo.ClamAv.Client;
using Lyo.ContentThreatScan.Abstractions;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.ContentThreatScan.ClamAv;

/// <summary>DI helpers that register <see cref="ClamAvContentThreatScan" />.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the scan from configuration section <see cref="ClamAvContentThreatScanOptions.SectionName" />.</summary>
        /// <remarks>Call <c>AddClamAvClient</c> first so <see cref="IClamAvClient" /> is available.</remarks>
        public IServiceCollection AddClamAvContentThreatScanFromConfiguration(
            IConfiguration configuration,
            string configSectionName = ClamAvContentThreatScanOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new ClamAvContentThreatScanOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddClamAvContentThreatScan(options);
        }

        /// <summary>Registers the scan and applies <paramref name="configure" /> before validation. Omit the callback to keep the defaults.</summary>
        /// <remarks>Call <c>AddClamAvClient</c> first so <see cref="IClamAvClient" /> is available.</remarks>
        public IServiceCollection AddClamAvContentThreatScan(Action<ClamAvContentThreatScanOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new ClamAvContentThreatScanOptions();
            configure?.Invoke(options);
            return services.AddClamAvContentThreatScan(options);
        }

        /// <summary>Registers the scan from a ready options instance. Calls <see cref="ClamAvContentThreatScanOptions.Validate" />.</summary>
        /// <remarks>Call <c>AddClamAvClient</c> first so <see cref="IClamAvClient" /> is available.</remarks>
        public IServiceCollection AddClamAvContentThreatScan(ClamAvContentThreatScanOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.AddSingleton(options);
            services.AddTransient(sp => new ClamAvContentThreatScan(
                sp.GetRequiredService<IClamAvClient>(), sp.GetRequiredService<ClamAvContentThreatScanOptions>(), sp.GetService<ILogger<ClamAvContentThreatScan>>()));

            services.AddTransient<IContentThreatReputationPipeline>(sp => sp.GetRequiredService<ClamAvContentThreatScan>());
            return services;
        }
    }
}
