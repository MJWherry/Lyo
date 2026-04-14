using Lyo.Exceptions;
using Lyo.Ffmpeg.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Ffmpeg;

/// <summary>Extension methods for registering FFmpeg services with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds FFmpeg services (AudioPlayer, AudioProber, AudioConverter) with default options.</summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>Call AddLyoMetrics() before this to enable metrics when FfmpegOptions.EnableMetrics is true.</remarks>
    public static IServiceCollection AddFfmpegServices(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddSingleton<FfmpegOptions>(_ => new());
        RegisterServices(services);
        return services;
    }

    /// <summary>Adds FFmpeg services with custom options.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFfmpegServices(this IServiceCollection services, Action<FfmpegOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton<FfmpegOptions>(_ => {
            var options = new FfmpegOptions();
            configure(options);
            return options;
        });

        RegisterServices(services);
        return services;
    }

    /// <summary>Adds FFmpeg services using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configSectionName">The configuration section name (defaults to "FfmpegOptions")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFfmpegServicesFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = FfmpegOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        services.AddSingleton<FfmpegOptions>(_ => {
            var options = new FfmpegOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return options;
        });

        RegisterServices(services);
        return services;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<FfmpegAudioPlayer>(provider => {
            var options = provider.GetRequiredService<FfmpegOptions>();
            var logger = provider.GetService<ILogger<FfmpegAudioPlayer>>();
            var metrics = provider.GetService<IMetrics>();
            return new(options, logger, metrics);
        });

        services.AddScoped<IAudioPlayer>(provider => provider.GetRequiredService<FfmpegAudioPlayer>());
        services.AddScoped<FfmpegAudioProber>(provider => {
            var options = provider.GetRequiredService<FfmpegOptions>();
            var logger = provider.GetService<ILogger<FfmpegAudioProber>>();
            var metrics = provider.GetService<IMetrics>();
            return new(options, logger, metrics);
        });

        services.AddScoped<IAudioProber>(provider => provider.GetRequiredService<FfmpegAudioProber>());
        services.AddScoped<FfmpegAudioConverter>(provider => {
            var options = provider.GetRequiredService<FfmpegOptions>();
            var logger = provider.GetService<ILogger<FfmpegAudioConverter>>();
            var metrics = provider.GetService<IMetrics>();
            return new(options, logger, metrics);
        });

        services.AddScoped<IAudioConverter>(provider => provider.GetRequiredService<FfmpegAudioConverter>());
    }
}