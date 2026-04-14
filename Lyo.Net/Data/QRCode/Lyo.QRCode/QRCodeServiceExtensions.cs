using Lyo.Exceptions;
using Lyo.Images;
using Lyo.Metrics;
using Lyo.QRCode.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.QRCode;

/// <summary>Registers <see cref="BuiltInQRCodeService" /> as <see cref="IQRCodeService" /> (in-library ISO encoder; PNG/SVG).</summary>
public static class QRCodeServiceExtensions
{
    /// <summary>Registers <see cref="BuiltInQRCodeService" /> as <see cref="IQRCodeService" />.</summary>
    public static IServiceCollection AddQRCodeService(this IServiceCollection services, Action<QRCodeServiceOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        var options = new QRCodeServiceOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        RegisterBuiltInQrCodeService(services);
        return services;
    }

    /// <summary>Registers <see cref="BuiltInQRCodeService" /> as <see cref="IQRCodeService" /> with explicit options.</summary>
    public static IServiceCollection AddQRCodeService(this IServiceCollection services, QRCodeServiceOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        RegisterBuiltInQrCodeService(services);
        return services;
    }

    /// <summary>Binds <see cref="QRCodeServiceOptions" /> from configuration and registers <see cref="BuiltInQRCodeService" /> as <see cref="IQRCodeService" />.</summary>
    public static IServiceCollection AddQRCodeServiceFromConfiguration(
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

        RegisterBuiltInQrCodeService(services);
        return services;
    }

    private static void RegisterBuiltInQrCodeService(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(IQrFrameLayoutService)))
            services.AddSingleton<IQrFrameLayoutService, QrFrameLayoutService>();

        services.AddSingleton<IQRCodeService>(sp =>
            new BuiltInQRCodeService(
                sp.GetRequiredService<QRCodeServiceOptions>(),
                sp.GetService<ILogger<BuiltInQRCodeService>>() ?? NullLogger<BuiltInQRCodeService>.Instance,
                sp.GetService<IMetrics>(),
                sp.GetService<IImageService>(),
                sp.GetRequiredService<IQrFrameLayoutService>()));
    }
}