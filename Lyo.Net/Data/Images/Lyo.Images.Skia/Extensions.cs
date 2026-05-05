using Lyo.Exceptions;
using Lyo.Images.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images.Skia;

/// <summary>Extension methods for SkiaSharp image service registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds SkiaSharp image service to the service collection.</summary>
        /// <param name="configure">Optional action to configure the options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddSkiaImageService(Action<ImageServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new ImageServiceOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IImageService, SkiaImageService>();
            return services;
        }

        /// <summary>Adds SkiaSharp image service to the service collection with explicit options.</summary>
        /// <param name="options">The image service options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddSkiaImageService(ImageServiceOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<IImageService, SkiaImageService>();
            return services;
        }

        /// <summary>Adds SkiaSharp image service to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration root.</param>
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
        public IServiceCollection AddSkiaImageServiceFromConfiguration(
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

            services.AddSingleton<IImageService, SkiaImageService>();
            return services;
        }
    }
}