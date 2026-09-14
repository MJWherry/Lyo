using Amazon;
using Amazon.Runtime;
using Amazon.Translate;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Translation.Aws;

/// <summary>DI helpers that register the AWS Translate service.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers AWS Translate by binding options from configuration.</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration).</param>
        /// <param name="configSectionName">Section to bind (defaults to "AwsTranslationOptions").</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddAwsTranslationServiceFromConfiguration(IConfiguration configuration, string configSectionName = AwsTranslationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            if (!services.Any(s => s.ServiceType == typeof(AwsTranslationOptions))) {
                var options = new AwsTranslationOptions();
                configuration.GetSection(configSectionName).Bind(options);
                options.Validate();
                services.AddSingleton(options);
            }

            // Bind IAmazonTranslate from configuration unless one is already present
            if (!services.Any(s => s.ServiceType == typeof(IAmazonTranslate))) {
                services.AddSingleton<IAmazonTranslate>(provider => {
                    var options = provider.GetRequiredService<AwsTranslationOptions>();

                    var config = new AmazonTranslateConfig();
                    if (!string.IsNullOrWhiteSpace(options.Region)) {
                        var region = RegionEndpoint.GetBySystemName(options.Region);
                        config.RegionEndpoint = region;
                    }

                    if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                        config.ServiceURL = options.ServiceUrl;

                    if (!string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey)) {
                        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
                        return new AmazonTranslateClient(credentials, config);
                    }

                    return new AmazonTranslateClient(config);
                });
            }

            // Add the translation service implementation
            services.AddSingleton<AwsTranslationService>(provider => {
                var options = provider.GetRequiredService<AwsTranslationOptions>();
                var logger = provider.GetService<ILogger<AwsTranslationService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var translateClient = provider.GetService<IAmazonTranslate>();
                return new(options, logger, metrics, translateClient);
            });

            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<AwsTranslationService>());
            return services;
        }

        /// <summary>Registers AWS Translate using a configuration callback.</summary>
        /// <param name="configure">Callback that receives the options object.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddAwsTranslationService(Action<AwsTranslationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new AwsTranslationOptions();
            configure(options);
            options.Validate();
            services.AddSingleton(options);

            services.AddSingleton<AwsTranslationService>(provider => {
                var options = provider.GetRequiredService<AwsTranslationOptions>();
                var logger = provider.GetService<ILogger<AwsTranslationService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var translateClient = provider.GetService<IAmazonTranslate>();
                return new(options, logger, metrics, translateClient);
            });

            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<AwsTranslationService>());
            return services;
        }

        /// <summary>Registers AWS Translate with a prepared options instance.</summary>
        /// <param name="options">AWS translation options to use.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddAwsTranslationService(AwsTranslationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<AwsTranslationService>(provider => {
                var logger = provider.GetService<ILogger<AwsTranslationService>>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var translateClient = provider.GetService<IAmazonTranslate>();
                return new(options, logger, metrics, translateClient);
            });

            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<AwsTranslationService>());
            return services;
        }
    }
}