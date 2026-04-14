#if WINDOWS || NETFRAMEWORK
using System;
using Lyo.Exceptions;
using Lyo.Tts;
using Lyo.Tts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Tts.WindowsSpeech;

/// <summary>Extension methods for Windows Speech TTS service registration.</summary>
public static class Extensions
{
    /// <summary>Adds Windows Speech TTS service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWindowsSpeechTtsService(
        this IServiceCollection services,
        Action<TtsServiceOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        var options = new TtsServiceOptions();
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<WindowsSpeechTtsService>(provider =>
        {
            var opts = provider.GetRequiredService<TtsServiceOptions>();
            var logger = provider.GetService<ILogger<WindowsSpeechTtsService>>();
            var metrics = provider.GetService<Lyo.Metrics.IMetrics>();
            return new WindowsSpeechTtsService(opts, logger, metrics);
        });
        services.AddSingleton<ITtsService<WindowsTtsRequest>>(provider => provider.GetRequiredService<WindowsSpeechTtsService>());
        return services;
    }

    /// <summary>Adds Windows Speech TTS service to the service collection with explicit options.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The TTS service options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWindowsSpeechTtsService(
        this IServiceCollection services,
        TtsServiceOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        
        services.AddSingleton(options);
        services.AddSingleton<WindowsSpeechTtsService>(provider =>
        {
            var logger = provider.GetService<ILogger<WindowsSpeechTtsService>>();
            var metrics = provider.GetService<Lyo.Metrics.IMetrics>();
            return new WindowsSpeechTtsService(options, logger, metrics);
        });
        services.AddSingleton<ITtsService<WindowsTtsRequest>>(provider => provider.GetRequiredService<WindowsSpeechTtsService>());
        return services;
    }
}
#endif

