using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.QRCode.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.QRCode;

/// <summary>DI helpers that register <see cref="BuiltInQRCodeService" /> as <see cref="IQRCodeService" /> (in-library ISO encoder; PNG/SVG).</summary>
public static class QRCodeServiceExtensions
{
    private static void RegisterBuiltInQrCodeService(IServiceCollection services)
        => services.AddSingleton<IQRCodeService>(sp => new BuiltInQRCodeService(
            sp.GetRequiredService<QRCodeServiceOptions>(), sp.GetService<ILogger<BuiltInQRCodeService>>() ?? NullLogger<BuiltInQRCodeService>.Instance, sp.GetService<IMetrics>()));

    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="BuiltInQRCodeService" /> as <see cref="IQRCodeService" />.</summary>
        public IServiceCollection AddQRCodeService(Action<QRCodeServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new QRCodeServiceOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            RegisterBuiltInQrCodeService(services);
            return services;
        }

        /// <summary>Registers <see cref="BuiltInQRCodeService" /> as <see cref="IQRCodeService" /> with caller-supplied options.</summary>
        public IServiceCollection AddQRCodeService(QRCodeServiceOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            RegisterBuiltInQrCodeService(services);
            return services;
        }

        /// <summary>Binds <see cref="QRCodeServiceOptions" /> from configuration and registers <see cref="BuiltInQRCodeService" /> as <see cref="IQRCodeService" />.</summary>
        public IServiceCollection AddQRCodeServiceFromConfiguration(IConfiguration configuration, string configSectionName = QRCodeServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(QRCodeServiceOptions))) {
                services.AddSingleton<QRCodeServiceOptions>(_ => {
                    var options = LyoOptions.Bind<QRCodeServiceOptions>(configuration, configSectionName);

                    return options;
                });
            }

            RegisterBuiltInQrCodeService(services);
            return services;
        }
    }
}