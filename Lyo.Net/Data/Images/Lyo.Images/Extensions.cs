using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Images.Models;
using Lyo.Images.Sprite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images;

/// <summary>DI helpers that register the ImageSharp image service.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="IImageDecorationService" /> if missing (overlay/frame/caption/padding can be used without <see cref="IImageService" />).
    /// </summary>
    private static void TryAddImageDecorationService(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(IImageDecorationService)))
            services.AddSingleton<IImageDecorationService>(sp => new ImageDecorationService(sp.GetService<ImageServiceOptions>()));
    }

    /// <summary>Registers <see cref="ISpriteSheetExportService" /> for spritesheet export and slicing.</summary>
    public static IServiceCollection AddSpriteSheetExportService(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddScoped<ISpriteSheetExportService, SpriteSheetExportService>();
        return services;
    }

    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the ImageSharp image service.</summary>
        /// <param name="configure">Optional options callback.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddImageSharpImageService(Action<ImageServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new ImageServiceOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IImageService, ImageSharpImageService>();
            TryAddImageDecorationService(services);
            return services;
        }

        /// <summary>Registers the ImageSharp image service with explicit options.</summary>
        /// <param name="options">Image service options.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddImageSharpImageService(ImageServiceOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<IImageService, ImageSharpImageService>();
            TryAddImageDecorationService(services);
            return services;
        }

        /// <summary>Registers the ImageSharp image service from configuration.</summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">Section name (starts as "ImageService"). Bound from IConfiguration when present.</param>
        /// <returns>Service collection for chaining.</returns>
        /// <remarks>
        /// <para>Binds from IConfiguration when that type is registered. Otherwise options keep their defaults.</para>
        /// <para>Example in appsettings.json:</para>
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
        public IServiceCollection AddImageSharpImageServiceFromConfiguration(IConfiguration configuration, string configSectionName = ImageServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(ImageServiceOptions))) {
                services.AddSingleton<ImageServiceOptions>(_ => {
                    var options = LyoOptions.Bind<ImageServiceOptions>(configuration, configSectionName);

                    return options;
                });
            }

            services.AddSingleton<IImageService, ImageSharpImageService>();
            TryAddImageDecorationService(services);
            return services;
        }
    }
}