using Amazon;
using Amazon.Polly;
using Amazon.Runtime;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Tts.AwsPolly;

/// <summary>DI helpers that register the AWS Polly TTS service.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers IAmazonPolly from configuration, reading AwsPollyOptions from the given section.</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration).</param>
        /// <param name="configSectionName">Section to bind (defaults to "AwsPollyOptions")</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddAmazonPollyFromConfiguration(IConfiguration configuration, string configSectionName = AwsPollyOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(AwsPollyOptions))) {
                var options = new AwsPollyOptions();
                configuration.GetSection(configSectionName).Bind(options);
                options.Validate();
                services.AddSingleton(options);
            }

            // Bind IAmazonPolly unless one is already present
            if (!services.Any(s => s.ServiceType == typeof(IAmazonPolly))) {
                services.AddSingleton<IAmazonPolly>(provider => {
                    var options = provider.GetRequiredService<AwsPollyOptions>();
                    var config = new AmazonPollyConfig();
                    if (!string.IsNullOrWhiteSpace(options.Region)) {
                        var region = RegionEndpoint.GetBySystemName(options.Region);
                        config.RegionEndpoint = region;
                    }

                    if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                        config.ServiceURL = options.ServiceUrl;

                    if (!string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey)) {
                        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
                        return new AmazonPollyClient(credentials, config);
                    }

                    // With no credentials, use the default credential chain
                    return new AmazonPollyClient(config);
                });
            }

            return services;
        }

        /// <summary>Registers AWS Polly TTS by binding options from configuration.</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration).</param>
        /// <param name="configSectionName">Section to bind (defaults to "AwsPollyOptions").</param>
        /// <returns>The same collection so further calls can chain.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        /// <exception cref="ArgumentException">Thrown when configSectionName is null or whitespace.</exception>
        /// <remarks>
        /// <para>Binds from IConfiguration when it is registered. If IConfiguration is missing, options keep their defaults.</para>
        /// <para>Sample appsettings.json:</para>
        /// <code>
        /// {
        ///   "AwsPollyOptions": {
        ///     "Region": "us-east-1",
        ///     "AccessKeyId": "your-access-key",
        ///     "SecretAccessKey": "your-secret-key",
        ///     "DefaultVoiceId": "Joanna",
        ///     "DefaultLanguageCode": "en-US",
        ///     "DefaultOutputFormat": "mp3"
        ///   }
        /// }
        /// </code>
        /// </remarks>
        public IServiceCollection AddAwsPollyTtsServiceFromConfiguration(IConfiguration configuration, string configSectionName = AwsPollyOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            // Bind IAmazonPolly from configuration unless already present
            if (!services.Any(s => s.ServiceType == typeof(IAmazonPolly)))
                services.AddAmazonPollyFromConfiguration(configuration, configSectionName);

            // Bind AwsPollyOptions from configuration unless already registered
            if (!services.Any(s => s.ServiceType == typeof(AwsPollyOptions))) {
                var options = new AwsPollyOptions();
                configuration.GetSection(configSectionName).Bind(options);
                options.Validate();
                services.AddSingleton(options);
            }

            // Add the TTS service implementation
            services.AddSingleton<AwsPollyTtsService>(provider => {
                var options = provider.GetRequiredService<AwsPollyOptions>();
                var logger = provider.GetService<ILogger<AwsPollyTtsService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var pollyClient = provider.GetService<IAmazonPolly>();
                return new(options, logger, metrics, pollyClient);
            });

            services.AddSingleton<ITtsService<AwsPollyTtsRequest>>(provider => provider.GetRequiredService<AwsPollyTtsService>());
            services.AddSingleton<AwsPollyTtsAppService>(provider => new(provider.GetRequiredService<AwsPollyTtsService>()));
            services.AddSingleton<ITtsService>(provider => provider.GetRequiredService<AwsPollyTtsAppService>());
            return services;
        }

        /// <summary>Registers AWS Polly TTS using an options callback.</summary>
        /// <param name="configure">Callback that receives the options object.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configure is null.</exception>
        public IServiceCollection AddAwsPollyTtsService(Action<AwsPollyOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new AwsPollyOptions();
            configure(options);
            options.Validate();
            services.AddSingleton(options);

            services.AddSingleton<AwsPollyTtsService>(provider => {
                var options = provider.GetRequiredService<AwsPollyOptions>();
                var logger = provider.GetService<ILogger<AwsPollyTtsService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var pollyClient = provider.GetService<IAmazonPolly>();
                return new(options, logger, metrics, pollyClient);
            });

            services.AddSingleton<ITtsService<AwsPollyTtsRequest>>(provider => provider.GetRequiredService<AwsPollyTtsService>());
            services.AddSingleton<AwsPollyTtsAppService>(provider => new(provider.GetRequiredService<AwsPollyTtsService>()));
            services.AddSingleton<ITtsService>(provider => provider.GetRequiredService<AwsPollyTtsAppService>());
            return services;
        }
    }
}