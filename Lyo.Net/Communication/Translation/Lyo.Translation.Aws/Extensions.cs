using Amazon;
using Amazon.Runtime;
using Amazon.Translate;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Translation.Aws;

/// <summary>Extension methods for registering AWS Translate service with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds AWS Translate service to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
    /// <param name="configSectionName">The configuration section name (defaults to "AwsTranslationOptions").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAwsTranslationServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = AwsTranslationOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

        // Register IAmazonTranslate from configuration if not already registered
        if (!services.Any(s => s.ServiceType == typeof(IAmazonTranslate))) {
            services.AddSingleton<IAmazonTranslate>(_ => {
                var options = new AwsTranslationOptions();
                var section = configuration.GetSection(configSectionName);
                if (section.Exists())
                    section.Bind(options);

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

        // Configure AwsTranslationOptions from configuration (if not already registered)
        if (!services.Any(s => s.ServiceType == typeof(AwsTranslationOptions))) {
            services.AddSingleton<AwsTranslationOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new AwsTranslationOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        // Register the translation service
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

    /// <summary>Adds AWS Translate service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action that receives the config object to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAwsTranslationService(this IServiceCollection services, Action<AwsTranslationOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton<AwsTranslationOptions>(_ => {
            var options = new AwsTranslationOptions();
            configure(options);
            return options;
        });

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

    /// <summary>Adds AWS Translate service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The AWS translation options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAwsTranslationService(this IServiceCollection services, AwsTranslationOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
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