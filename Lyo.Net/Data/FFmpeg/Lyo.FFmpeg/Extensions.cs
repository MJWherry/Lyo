using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FFmpeg;

/// <summary>Extension methods for registering FFmpeg services with dependency injection.</summary>
public static class Extensions
{
    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<FFmpegAudioPlayer>(provider => {
            var options = provider.GetRequiredService<FFmpegOptions>();
            var logger = provider.GetService<ILogger<FFmpegAudioPlayer>>();
            var metrics = provider.GetService<IMetrics>();
            return new(options, logger, metrics);
        });

        services.AddScoped<IAudioPlayer>(provider => provider.GetRequiredService<FFmpegAudioPlayer>());
        services.AddScoped<FFmpegAudioProber>(provider => {
            var options = provider.GetRequiredService<FFmpegOptions>();
            var logger = provider.GetService<ILogger<FFmpegAudioProber>>();
            var metrics = provider.GetService<IMetrics>();
            return new(options, logger, metrics);
        });

        services.AddScoped<IAudioProber>(provider => provider.GetRequiredService<FFmpegAudioProber>());
        services.AddScoped<FFmpegAudioConverter>(provider => {
            var options = provider.GetRequiredService<FFmpegOptions>();
            var logger = provider.GetService<ILogger<FFmpegAudioConverter>>();
            var metrics = provider.GetService<IMetrics>();
            return new(options, logger, metrics);
        });

        services.AddScoped<IAudioConverter>(provider => provider.GetRequiredService<FFmpegAudioConverter>());
    }

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds FFmpeg services (AudioPlayer, AudioProber, AudioConverter) with default options.</summary>
        /// <returns>The service collection for chaining</returns>
        /// <remarks>Call AddLyoMetrics() before this to enable metrics when FFmpegOptions.EnableMetrics is true.</remarks>
        public IServiceCollection AddFFmpegServices()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<FFmpegOptions>(_ => new());
            RegisterServices(services);
            return services;
        }

        /// <summary>Adds FFmpeg services with custom options.</summary>
        /// <param name="configure">Action to configure the options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFFmpegServices(Action<FFmpegOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<FFmpegOptions>(_ => {
                var options = new FFmpegOptions();
                configure(options);
                return options;
            });

            RegisterServices(services);
            return services;
        }

        /// <summary>Adds FFmpeg services using configuration binding.</summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="configSectionName">The configuration section name (defaults to "FFmpegOptions")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFFmpegServicesFromConfiguration(IConfiguration configuration, string configSectionName = FFmpegOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddSingleton<FFmpegOptions>(_ => {
                var options = new FFmpegOptions();
                var section = configuration.GetSection(configSectionName);
                if (section.Exists())
                    section.Bind(options);

                return options;
            });

            RegisterServices(services);
            return services;
        }
    }
}