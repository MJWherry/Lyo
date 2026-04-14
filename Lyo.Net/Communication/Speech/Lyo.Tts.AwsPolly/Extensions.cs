using Amazon;
using Amazon.Polly;
using Amazon.Runtime;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Tts.AwsPolly;

/// <summary>Extension methods for registering AWS Polly TTS service with dependency injection.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers IAmazonPolly from configuration. Reads AwsPollyOptions from the specified configuration section.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">The configuration section name (defaults to "AwsPollyOptions")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAmazonPollyFromConfiguration(IConfiguration configuration, string configSectionName = AwsPollyOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
            if (!services.Any(s => s.ServiceType == typeof(AwsPollyOptions))) {
                services.AddSingleton<AwsPollyOptions>(_ => {
                    var section = configuration.GetSection(configSectionName);
                    var options = new AwsPollyOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }

            // Register IAmazonPolly if not already registered
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

                    // If no credentials provided, use default credential chain
                    return new AmazonPollyClient(config);
                });
            }

            return services;
        }

        /// <summary>Adds AWS Polly TTS service to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">The configuration section name (defaults to "AwsPollyOptions").</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        /// <exception cref="ArgumentException">Thrown when configSectionName is null or whitespace.</exception>
        /// <remarks>
        /// <para>This method binds configuration from IConfiguration if it's registered in the service collection. If IConfiguration is not available, the options will use default values.</para>
        /// <para>Example configuration in appsettings.json:</para>
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
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

            // Register IAmazonPolly from configuration if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonPolly)))
                services.AddAmazonPollyFromConfiguration(configuration, configSectionName);

            // Configure AwsPollyOptions from configuration (if not already registered)
            if (!services.Any(s => s.ServiceType == typeof(AwsPollyOptions))) {
                services.AddSingleton<AwsPollyOptions>(_ => {
                    var section = configuration.GetSection(configSectionName);
                    var options = new AwsPollyOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }

            // Register the TTS service
            services.AddSingleton<AwsPollyTtsService>(provider => {
                var options = provider.GetRequiredService<AwsPollyOptions>();
                var logger = provider.GetService<ILogger<AwsPollyTtsService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var pollyClient = provider.GetService<IAmazonPolly>();
                return new(options, logger, metrics, pollyClient);
            });

            services.AddSingleton<ITtsService<AwsPollyTtsRequest>>(provider => provider.GetRequiredService<AwsPollyTtsService>());
            return services;
        }

        /// <summary>Adds AWS Polly TTS service to the service collection.</summary>
        /// <param name="configure">Action that receives the config object to configure.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configure is null.</exception>
        public IServiceCollection AddAwsPollyTtsService(Action<AwsPollyOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<AwsPollyOptions>(_ => {
                var options = new AwsPollyOptions();
                configure(options);
                return options;
            });

            services.AddSingleton<AwsPollyTtsService>(provider => {
                var options = provider.GetRequiredService<AwsPollyOptions>();
                var logger = provider.GetService<ILogger<AwsPollyTtsService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var pollyClient = provider.GetService<IAmazonPolly>();
                return new(options, logger, metrics, pollyClient);
            });

            services.AddSingleton<ITtsService<AwsPollyTtsRequest>>(provider => provider.GetRequiredService<AwsPollyTtsService>());
            return services;
        }
    }
}