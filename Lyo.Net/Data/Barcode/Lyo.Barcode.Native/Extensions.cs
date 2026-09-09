using Lyo.Barcode.Models;
using Lyo.Configuration;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Barcode.Native;

/// <summary>DI helpers that register <see cref="NativeBarcodeService" />.</summary>
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

        /// <param name="configuration">Host configuration, typically <c>builder.Configuration</c>.</param>
        /// <param name="configSectionName">Section to bind. Starts as <see cref="BarcodeServiceOptions.SectionName" />.</param>
        public IServiceCollection AddNativeBarcodeServiceFromConfiguration(IConfiguration configuration, string configSectionName = BarcodeServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(BarcodeServiceOptions))) {
                services.AddSingleton<BarcodeServiceOptions>(_ => {
                    var options = LyoOptions.Bind<BarcodeServiceOptions>(configuration, configSectionName);

                    return options;
                });
            }

            services.AddSingleton<IBarcodeService, NativeBarcodeService>();
            return services;
        }
    }
}