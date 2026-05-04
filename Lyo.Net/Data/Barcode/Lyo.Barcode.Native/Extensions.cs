using Lyo.Barcode.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Barcode.Native;

/// <summary>DI registration for <see cref="NativeBarcodeService" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddNativeBarcodeService(Action<BarcodeServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new BarcodeServiceOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IBarcodeService, NativeBarcodeService>();
            return services;
        }

        public IServiceCollection AddNativeBarcodeService(BarcodeServiceOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<IBarcodeService, NativeBarcodeService>();
            return services;
        }

        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">Defaults to <see cref="BarcodeServiceOptions.SectionName" />.</param>
        public IServiceCollection AddNativeBarcodeServiceFromConfiguration(IConfiguration configuration, string configSectionName = BarcodeServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(BarcodeServiceOptions))) {
                services.AddSingleton<BarcodeServiceOptions>(_ => {
                    var section = configuration.GetSection(configSectionName);
                    var options = new BarcodeServiceOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }

            services.AddSingleton<IBarcodeService, NativeBarcodeService>();
            return services;
        }
    }
}