using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Images.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images.Skia;

/// <summary>DI helpers that register the SkiaSharp image service.</summary>
public static class Extensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the SkiaSharp image service.</summary>
        /// <param name="configure">Optional callback that mutates options.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSkiaImageService(Action<ImageServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new ImageServiceOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IImageService, SkiaImageService>();
            return services;
        }

        /// <summary>Registers the SkiaSharp image service from an options instance.</summary>
        /// <param name="options">Image service options.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSkiaImageService(ImageServiceOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<IImageService, SkiaImageService>();
            return services;
        }

        /// <summary>Registers the SkiaSharp image service by binding configuration.</summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">Section name; starts as "ImageService". Bound from IConfiguration when available.</param>
        /// <returns>Service collection for chaining.</returns>
        /// <remarks>
        /// <para>Binds from IConfiguration when that type is registered; otherwise options keep their starting values.</para>
        /// <para>Sample appsettings.json:</para>
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
        public IServiceCollection AddSkiaImageServiceFromConfiguration(IConfiguration configuration, string configSectionName = ImageServiceOptions.SectionName)
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

            services.AddSingleton<IImageService, SkiaImageService>();
            return services;
        }
    }
}