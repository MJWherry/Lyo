#if WINDOWS || NETFRAMEWORK
using System;
using Lyo.Exceptions;
using Lyo.Tts;
using Lyo.Tts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Tts.WindowsSpeech;

/// <summary>DI helpers that register Windows Speech TTS.</summary>
public static class Extensions
{
    /// <summary>Registers Windows Speech TTS on the collection.</summary>
    /// <param name="services">Service collection to add registrations to.</param>
    /// <param name="configure">Optional callback that mutates options.</param>
    /// <returns>The same collection so further calls can chain.</returns>
    public static IServiceCollection AddWindowsSpeechTtsService(
        this IServiceCollection services,
        Action<TtsServiceOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
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

    /// <summary>Registers Windows Speech TTS with a prepared options instance.</summary>
    /// <param name="services">Service collection to add registrations to.</param>
    /// <param name="options">TTS options to use.</param>
    /// <returns>The same collection so further calls can chain.</returns>
    public static IServiceCollection AddWindowsSpeechTtsService(
        this IServiceCollection services,
        TtsServiceOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        
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