using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.QRCode.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.QRCode.QRCoder;

/// <summary>DI helpers that register <see cref="QRCoderQRCodeService" /> (QRCoder NuGet) as <see cref="IQRCodeService" />.</summary>
public static class QRCoderQrCodeServiceExtensions
{
    private static void RegisterQRCoderQrCodeService(IServiceCollection services)
        => services.AddSingleton<IQRCodeService>(sp => new QRCoderQRCodeService(
            sp.GetRequiredService<QRCodeServiceOptions>(), sp.GetService<ILogger<QRCoderQRCodeService>>() ?? NullLogger<QRCoderQRCodeService>.Instance, sp.GetService<IMetrics>()));

    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="QRCoderQRCodeService" /> for <see cref="IQRCodeService" />.</summary>
        public IServiceCollection AddQRCoderQrCodeService(Action<QRCodeServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new QRCodeServiceOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            RegisterQRCoderQrCodeService(services);
            return services;
        }

        /// <summary>Registers <see cref="QRCoderQRCodeService" /> for <see cref="IQRCodeService" /> from an options instance.</summary>
        public IServiceCollection AddQRCoderQrCodeService(QRCodeServiceOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            RegisterQRCoderQrCodeService(services);
            return services;
        }

        /// <summary>Binds <see cref="QRCodeServiceOptions" /> from configuration and registers <see cref="QRCoderQRCodeService" /> for <see cref="IQRCodeService" />.</summary>
        /// <remarks>
        /// <para>Sample appsettings.json:</para>
        /// <code>
        /// {
        ///   "QRCodeService": {
        ///     "DefaultSize": 256,
        ///     "DefaultFormat": "Png",
        ///     "DefaultErrorCorrectionLevel": "Medium",
        ///     "MinSize": 1,
        ///     "MaxSize": 2000,
        ///     "EnableMetrics": false
        ///   }
        /// }
        /// </code>
        /// </remarks>
        public IServiceCollection AddQRCoderQrCodeServiceFromConfiguration(IConfiguration configuration, string configSectionName = QRCodeServiceOptions.SectionName)
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

            RegisterQRCoderQrCodeService(services);
            return services;
        }
    }
}