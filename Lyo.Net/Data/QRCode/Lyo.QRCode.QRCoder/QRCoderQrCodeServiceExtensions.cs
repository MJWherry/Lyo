using Lyo.Exceptions;
using Lyo.Images;
using Lyo.Metrics;
using Lyo.QRCode;
using Lyo.QRCode.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.QRCode.QRCoder;

/// <summary>Registers <see cref="QRCoderQRCodeService" /> (QRCoder NuGet) as <see cref="IQRCodeService" />.</summary>
public static class QRCoderQrCodeServiceExtensions
{
    /// <summary>Registers <see cref="QRCoderQRCodeService" /> as <see cref="IQRCodeService" />.</summary>
    public static IServiceCollection AddQRCoderQrCodeService(this IServiceCollection services, Action<QRCodeServiceOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        var options = new QRCodeServiceOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        RegisterQRCoderQrCodeService(services);
        return services;
    }

    /// <summary>Registers <see cref="QRCoderQRCodeService" /> as <see cref="IQRCodeService" /> with explicit options.</summary>
    public static IServiceCollection AddQRCoderQrCodeService(this IServiceCollection services, QRCodeServiceOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        RegisterQRCoderQrCodeService(services);
        return services;
    }

    /// <summary>Binds <see cref="QRCodeServiceOptions" /> from configuration and registers <see cref="QRCoderQRCodeService" /> as <see cref="IQRCodeService" />.</summary>
    /// <remarks>
    /// <para>Example configuration in appsettings.json:</para>
    /// <code>
    /// {
    ///   "QRCodeService": {
    ///     "DefaultSize": 256,
    ///     "DefaultFormat": "Png",
    ///     "DefaultErrorCorrectionLevel": "Medium",
    ///     "MinSize": 50,
    ///     "MaxSize": 2000,
    ///     "EnableMetrics": false
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddQRCoderQrCodeServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = QRCodeServiceOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        if (!services.Any(s => s.ServiceType == typeof(QRCodeServiceOptions))) {
            services.AddSingleton<QRCodeServiceOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new QRCodeServiceOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        RegisterQRCoderQrCodeService(services);
        return services;
    }

    private static void RegisterQRCoderQrCodeService(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(IQrFrameLayoutService)))
            services.AddSingleton<IQrFrameLayoutService, QrFrameLayoutService>();

        services.AddSingleton<IQRCodeService>(sp =>
            new QRCoderQRCodeService(
                sp.GetRequiredService<QRCodeServiceOptions>(),
                sp.GetService<ILogger<QRCoderQRCodeService>>() ?? NullLogger<QRCoderQRCodeService>.Instance,
                sp.GetService<IMetrics>(),
                sp.GetService<IImageService>(),
                sp.GetRequiredService<IQrFrameLayoutService>()));
    }
}