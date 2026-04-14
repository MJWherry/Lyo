using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Typecast.Client;
using Lyo.Typecast.Client.Models.TextToSpeech.Request;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Tts.Typecast;

/// <summary>Extension methods for registering Typecast TTS service with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds Typecast TTS service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action that receives the config object to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>Requires TypecastClient to be registered first using AddTypecastClientFromConfiguration() or AddTypecastClient().</para>
    /// </remarks>
    public static IServiceCollection AddTypecastTtsService(this IServiceCollection services, Action<TypecastOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        // Configure TypecastOptions (if not already registered)
        if (!services.Any(s => s.ServiceType == typeof(TypecastOptions))) {
            services.AddSingleton<TypecastOptions>(_ => {
                var options = new TypecastOptions();
                configure?.Invoke(options);
                return options;
            });
        }

        // Register the TTS service (requires TypecastClient to be registered)
        services.AddSingleton<TypecastTtsService>(provider => {
            var typecastClient = provider.GetRequiredService<TypecastClient>();
            var options = provider.GetRequiredService<TypecastOptions>();
            var logger = provider.GetService<ILogger<TypecastTtsService>>();
            var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            return new(typecastClient, options, logger, metrics);
        });

        services.AddSingleton<ITtsService<TypecastTtsRequest>>(provider => provider.GetRequiredService<TypecastTtsService>());
        return services;
    }

    /// <summary>Adds Typecast TTS service to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configSectionName">The configuration section name (defaults to "TypecastOptions").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>Requires TypecastClient to be registered first using AddTypecastClientFromConfiguration() or AddTypecastClient().</para>
    /// <para>Example configuration in appsettings.json:</para>
    /// <code>
    /// {
    ///   "TypecastClient": {
    ///     "ApiKey": "your-api-key",
    ///     "ApiBaseUrl": "https://api.typecast.ai"
    ///   },
    ///   "TypecastOptions": {
    ///     "DefaultVoiceId": "voice-id",
    ///     "DefaultLanguageCode": "en-US",
    ///     "DefaultOutputFormat": "mp3",
    ///     "DefaultModel": "SsfmV30"
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddTypecastTtsServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = TypecastOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

        // Configure TypecastOptions from configuration (if not already registered)
        if (!services.Any(s => s.ServiceType == typeof(TypecastOptions))) {
            services.AddSingleton<TypecastOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new TypecastOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        // Register the TTS service (requires TypecastClient to be registered)
        services.AddSingleton<TypecastTtsService>(provider => {
            var typecastClient = provider.GetRequiredService<TypecastClient>();
            var options = provider.GetRequiredService<TypecastOptions>();
            var logger = provider.GetService<ILogger<TypecastTtsService>>();
            var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            return new(typecastClient, options, logger, metrics);
        });

        services.AddSingleton<ITtsService<TypecastTtsRequest>>(provider => provider.GetRequiredService<TypecastTtsService>());
        return services;
    }
}