using Lyo.Exceptions;
using Lyo.Images.Models;
using Lyo.Images.Sprite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images;

/// <summary>Extension methods for ImageSharp image service registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds ImageSharp image service to the service collection.</summary>
        /// <param name="configure">Optional action to configure the options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddImageSharpImageService(Action<ImageServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new ImageServiceOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IImageService, ImageSharpImageService>();
            TryAddQrFrameLayoutService(services);
            return services;
        }

        /// <summary>Adds ImageSharp image service to the service collection with explicit options.</summary>
        /// <param name="options">The image service options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddImageSharpImageService(ImageServiceOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<IImageService, ImageSharpImageService>();
            TryAddQrFrameLayoutService(services);
            return services;
        }

        /// <summary>Adds ImageSharp image service to the service collection using configuration binding.</summary>
        /// <param name="configSectionName">The configuration section name (defaults to "ImageService"). The section will be bound from IConfiguration if available.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>This method binds configuration from IConfiguration if it's registered in the service collection. If IConfiguration is not available, the options will use default values.</para>
        /// <para>Example configuration in appsettings.json:</para>
        /// <code>
        /// {
        ///   "ImageService": {
        ///     "DefaultQuality": 90,
        ///     "MaxWidth": 10000,
        ///     "MaxHeight": 10000,
        ///     "MaxFileSizeBytes": 104857600,
        ///     "EnableMetrics": false
        ///   }
        /// }
        /// </code>
        /// </remarks>
        public IServiceCollection AddImageSharpImageServiceFromConfiguration(
            IConfiguration configuration,
            string configSectionName = ImageServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(ImageServiceOptions))) {
                services.AddSingleton<ImageServiceOptions>(_ => {
                    var section = configuration.GetSection(configSectionName);
                    var options = new ImageServiceOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }

            services.AddSingleton<IImageService, ImageSharpImageService>();
            TryAddQrFrameLayoutService(services);
            return services;
        }
    }

    /// <summary>Registers <see cref="IQrFrameLayoutService" /> when not already present (QR frame compositing only needs ImageSharp + fonts).</summary>
    private static void TryAddQrFrameLayoutService(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(IQrFrameLayoutService)))
            services.AddSingleton<IQrFrameLayoutService, QrFrameLayoutService>();
    }

    /// <summary>Registers <see cref="ISpriteSheetExportService" /> for spritesheet export and slicing helpers.</summary>
    public static IServiceCollection AddSpriteSheetExportService(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddScoped<ISpriteSheetExportService, SpriteSheetExportService>();
        return services;
    }
}