using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.FFmpeg;

/// <summary>DI helpers that register FFmpeg audio and video player, prober, and converter services.</summary>
public static class Extensions
{
    /// <param name="services">Collection to add the FFmpeg services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers audio and video player, prober, and converter with stock options.</summary>
        public IServiceCollection AddFFmpegServices() => services.AddFFmpegServices(new FFmpegOptions());

        /// <summary>Registers FFmpeg services and runs <paramref name="configure" /> on the options instance.</summary>
        public IServiceCollection AddFFmpegServices(Action<FFmpegOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new FFmpegOptions();
            configure(options);
            return services.AddFFmpegServices(options);
        }

        /// <summary>Registers FFmpeg services with options bound from configuration.</summary>
        public IServiceCollection AddFFmpegServicesFromConfiguration(IConfiguration configuration, string configSectionName = FFmpegOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<FFmpegOptions>(configuration, configSectionName);
            return services.AddFFmpegServices(options);
        }

        /// <summary>Registers FFmpeg audio and video services with the given options instance.</summary>
        public IServiceCollection AddFFmpegServices(FFmpegOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<FFmpegOptions>>().Value);
            RegisterAudio(services);
            RegisterVideo(services);
            return services;
        }
    }

    private static void RegisterAudio(IServiceCollection services)
    {
        services.TryAddScoped<FFmpegAudioConverter>(sp => {
            var opts = sp.GetRequiredService<FFmpegOptions>();
            var logger = sp.GetService<ILogger<FFmpegAudioConverter>>();
            var metrics = sp.GetService<IMetrics>();
            return new(opts, logger, metrics);
        });
        services.TryAddScoped<IAudioConverter>(sp => sp.GetRequiredService<FFmpegAudioConverter>());
        services.TryAddScoped<FFmpegAudioProber>(sp => {
            var opts = sp.GetRequiredService<FFmpegOptions>();
            var logger = sp.GetService<ILogger<FFmpegAudioProber>>();
            var metrics = sp.GetService<IMetrics>();
            return new(opts, logger, metrics);
        });
        services.TryAddScoped<IAudioProber>(sp => sp.GetRequiredService<FFmpegAudioProber>());
        services.TryAddScoped<FFmpegAudioPlayer>(sp => {
            var opts = sp.GetRequiredService<FFmpegOptions>();
            var logger = sp.GetService<ILogger<FFmpegAudioPlayer>>();
            var metrics = sp.GetService<IMetrics>();
            return new(opts, logger, metrics);
        });
        services.TryAddScoped<IAudioPlayer>(sp => sp.GetRequiredService<FFmpegAudioPlayer>());
    }

    private static void RegisterVideo(IServiceCollection services)
    {
        services.TryAddScoped<FFmpegVideoConverter>(sp => {
            var opts = sp.GetRequiredService<FFmpegOptions>();
            var logger = sp.GetService<ILogger<FFmpegVideoConverter>>();
            var metrics = sp.GetService<IMetrics>();
            return new(opts, logger, metrics);
        });
        services.TryAddScoped<IVideoConverter>(sp => sp.GetRequiredService<FFmpegVideoConverter>());
        services.TryAddScoped<FFmpegVideoProber>(sp => {
            var opts = sp.GetRequiredService<FFmpegOptions>();
            var logger = sp.GetService<ILogger<FFmpegVideoProber>>();
            var metrics = sp.GetService<IMetrics>();
            return new(opts, logger, metrics);
        });
        services.TryAddScoped<IVideoProber>(sp => sp.GetRequiredService<FFmpegVideoProber>());
        services.TryAddScoped<FFmpegVideoPlayer>(sp => {
            var opts = sp.GetRequiredService<FFmpegOptions>();
            var logger = sp.GetService<ILogger<FFmpegVideoPlayer>>();
            var metrics = sp.GetService<IMetrics>();
            return new(opts, logger, metrics);
        });
        services.TryAddScoped<IVideoPlayer>(sp => sp.GetRequiredService<FFmpegVideoPlayer>());
    }
}
