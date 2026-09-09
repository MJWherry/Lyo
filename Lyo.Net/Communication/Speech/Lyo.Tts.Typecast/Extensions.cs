using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Typecast.Client;
using Lyo.Typecast.Client.Models.TextToSpeech.Request;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Tts.Typecast;

/// <summary>DI helpers that register the Typecast TTS service.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers Typecast TTS using an options callback.</summary>
        /// <param name="configure">Callback that receives the options object.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        /// <remarks>
        /// <para>Register TypecastClient first via AddTypecastClientFromConfiguration() or AddTypecastClient().</para>
        /// </remarks>
        public IServiceCollection AddTypecastTtsService(Action<TypecastOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            // Bind TypecastOptions unless already registered
            if (!services.Any(s => s.ServiceType == typeof(TypecastOptions))) {
                services.AddSingleton<TypecastOptions>(_ => {
                    var options = new TypecastOptions();
                    configure?.Invoke(options);
                    return options;
                });
            }

            // Add the TTS service (TypecastClient must already be registered)
            services.AddSingleton<TypecastTtsService>(provider => {
                var typecastClient = provider.GetRequiredService<TypecastClient>();
                var options = provider.GetRequiredService<TypecastOptions>();
                var logger = provider.GetService<ILogger<TypecastTtsService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new(typecastClient, options, logger, metrics);
            });

            services.AddSingleton<ITtsService<TypecastTtsRequest>>(provider => provider.GetRequiredService<TypecastTtsService>());
            services.AddSingleton<TypecastTtsAppService>(provider => new(provider.GetRequiredService<TypecastTtsService>()));
            services.AddSingleton<ITtsService>(provider => provider.GetRequiredService<TypecastTtsAppService>());
            return services;
        }

        /// <summary>Registers Typecast TTS by binding options from configuration.</summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">Section to bind (defaults to "TypecastOptions").</param>
        /// <returns>The same collection so further calls can chain.</returns>
        /// <remarks>
        /// <para>Register TypecastClient first via AddTypecastClientFromConfiguration() or AddTypecastClient().</para>
        /// <para>Sample appsettings.json:</para>
        /// <code>
        /// {
        ///   "TypecastClient": {
        ///     "ApiKey": "your-api-key",
        ///     "BaseUrl": "https://api.typecast.ai"
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
        public IServiceCollection AddTypecastTtsServiceFromConfiguration(IConfiguration configuration, string configSectionName = TypecastOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            // Bind TypecastOptions from configuration unless already registered
            if (!services.Any(s => s.ServiceType == typeof(TypecastOptions))) {
                services.AddSingleton<TypecastOptions>(_ => {
                    var options = LyoOptions.Bind<TypecastOptions>(configuration, configSectionName);

                    return options;
                });
            }

            // Add the TTS implementation (TypecastClient must already be registered)
            services.AddSingleton<TypecastTtsService>(provider => {
                var typecastClient = provider.GetRequiredService<TypecastClient>();
                var options = provider.GetRequiredService<TypecastOptions>();
                var logger = provider.GetService<ILogger<TypecastTtsService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new(typecastClient, options, logger, metrics);
            });

            services.AddSingleton<ITtsService<TypecastTtsRequest>>(provider => provider.GetRequiredService<TypecastTtsService>());
            services.AddSingleton<TypecastTtsAppService>(provider => new(provider.GetRequiredService<TypecastTtsService>()));
            services.AddSingleton<ITtsService>(provider => provider.GetRequiredService<TypecastTtsAppService>());
            return services;
        }
    }
}