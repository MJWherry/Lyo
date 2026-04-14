using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.S3;

/// <summary>Backblaze B2 uses the same S3 API client as AWS; these helpers set the standard B2 S3 endpoint from <see cref="S3FileStorageOptions.Region" />.</summary>
public static class S3FileStorageBackblazeExtensions
{
    /// <summary>Configuration section name for B2 (same property names as <see cref="S3FileStorageOptions" />: bucket, region, keys, etc.).</summary>
    public const string BackblazeFileStorageConfigurationSectionName = "BackblazeFileStorage";

    /// <summary>
    /// Sets <see cref="S3FileStorageOptions.ServiceUrl" /> to the B2 S3 endpoint for <see cref="S3FileStorageOptions.Region" /> when <see cref="S3FileStorageOptions.ServiceUrl" />
    /// is not already set. Use B2 application key id and key as <see cref="S3FileStorageOptions.AccessKeyId" /> and <see cref="S3FileStorageOptions.SecretAccessKey" />.
    /// </summary>
    public static void ApplyBackblazeB2Defaults(this S3FileStorageOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl) || string.IsNullOrWhiteSpace(options.Region))
            return;

        options.ServiceUrl = GetS3EndpointForBackblazeRegion(options.Region);
    }

    /// <summary>Returns the S3-compatible API URL for a B2 region (e.g. <c>us-west-004</c>).</summary>
    public static string GetS3EndpointForBackblazeRegion(string b2Region)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(b2Region, nameof(b2Region));
        return $"https://s3.{b2Region.Trim()}.backblazeb2.com";
    }

    /// <summary>
    /// Registers <see cref="S3FileStorageOptions" /> from configuration, applies <see cref="ApplyBackblazeB2Defaults" />, then returns the same builder as
    /// <see cref="Extensions.AddS3FileStorageServiceKeyed" />.
    /// </summary>
    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForBackblaze(
        this IServiceCollection services,
        string keyName,
        IConfiguration configuration,
        string configSectionName = BackblazeFileStorageConfigurationSectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        RegisterBackblazeS3OptionsFromConfiguration(services, configuration, configSectionName);
        return services.AddS3FileStorageServiceKeyed(keyName);
    }

    /// <summary>Configures <see cref="S3FileStorageOptions" /> in code and applies <see cref="ApplyBackblazeB2Defaults" />.</summary>
    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForBackblaze(this IServiceCollection services, string keyName, Action<S3FileStorageOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        if (!services.Any(s => s.ServiceType == typeof(S3FileStorageOptions))) {
            services.AddSingleton<S3FileStorageOptions>(_ => {
                var options = new S3FileStorageOptions();
                configure(options);
                options.ApplyBackblazeB2Defaults();
                return options;
            });
        }

        return services.AddS3FileStorageServiceKeyed(keyName);
    }

    /// <summary>Same keyed multipart registration as <see cref="Extensions.AddKeyedS3MultipartUploadService" />; kept for discoverability when using B2.</summary>
    public static IServiceCollection AddKeyedS3MultipartUploadServiceForBackblaze(this IServiceCollection services, string serviceKey)
        => services.AddKeyedS3MultipartUploadService(serviceKey);

    private static void RegisterBackblazeS3OptionsFromConfiguration(IServiceCollection services, IConfiguration configuration, string configSectionName)
    {
        if (services.Any(s => s.ServiceType == typeof(S3FileStorageOptions)))
            return;

        services.AddSingleton<S3FileStorageOptions>(_ => {
            var section = configuration.GetSection(configSectionName);
            var options = new S3FileStorageOptions();
            if (section.Exists())
                section.Bind(options);

            options.ApplyBackblazeB2Defaults();
            return options;
        });
    }
}
