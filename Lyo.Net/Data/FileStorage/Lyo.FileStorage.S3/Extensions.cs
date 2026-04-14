using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Lyo.Exceptions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.S3.Multipart;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.S3;

public static class Extensions
{
    /// <summary>
    /// Registers <see cref="S3MultipartUploadService" /> as a keyed <see cref="IMultipartUploadService" /> (same key as keyed <see cref="S3FileStorageService" />).
    /// Usually called automatically by <see cref="S3FileStorageServiceBuilder.Build" />; call this only to register multipart alone or to replace the default registration.
    /// </summary>
    public static IServiceCollection AddKeyedS3MultipartUploadService(this IServiceCollection services, string serviceKey)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(serviceKey, nameof(serviceKey));
        services.AddKeyedScoped<S3MultipartUploadService>(
            serviceKey, (provider, _) => {
                var opts = provider.GetRequiredService<S3FileStorageOptions>();
                var metrics = opts.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new(
                    provider.GetRequiredKeyedService<S3FileStorageService>(serviceKey), opts, provider.GetRequiredService<IAmazonS3>(),
                    provider.GetRequiredService<IMultipartUploadSessionStore>(), provider.GetService<IFileMalwareScanner>(), provider.GetServices<IFileAuditEventHandler>(),
                    provider.GetService<IFileOperationContextAccessor>(), provider.GetService<ILoggerFactory>(), metrics);
            });

        services.AddKeyedScoped<IMultipartUploadService>(serviceKey, (provider, _) => provider.GetRequiredKeyedService<S3MultipartUploadService>(serviceKey));
        return services;
    }

    /// <summary>
    /// Same as <see cref="AddKeyedS3MultipartUploadService" /> — alias for callers who think in terms of the AWS S3 API (works for any S3-compatible endpoint:
    /// AWS, MinIO, Wasabi, R2, etc.).
    /// </summary>
    public static IServiceCollection AddKeyedAwsMultipartUploadService(this IServiceCollection services, string serviceKey)
        => services.AddKeyedS3MultipartUploadService(serviceKey);

    /// <summary>Adds a keyed S3 file storage service (AWS, Backblaze B2, MinIO, etc.) to the service collection using a builder pattern.</summary>
    /// <param name="keyName">The key name for the keyed file storage service</param>
    /// <returns>A builder for configuring the service and its dependencies</returns>
    /// <example>
    /// <code>
    /// // Use existing keyed services:
    /// services.AddS3FileStorageServiceKeyed("client-files")
    ///     .UseFileMetadataStore("postgres-filemetadatastore")
    ///     .UseEncryptionService("two-key-aws")
    ///     .ConfigureS3FileStorage("S3FileStorageOptions")
    ///     .Build(configuration);
    /// </code>
    /// </example>
    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyed(this IServiceCollection services, string keyName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        return new(services, keyName);
    }

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers IAmazonS3 from configuration. Binds <see cref="S3FileStorageOptions" /> from the specified configuration section.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">The configuration section name (defaults to <see cref="S3FileStorageOptions.SectionName" />).</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAmazonS3FromConfiguration(IConfiguration configuration, string configSectionName = S3FileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
            if (!services.Any(s => s.ServiceType == typeof(S3FileStorageOptions))) {
                services.AddSingleton<S3FileStorageOptions>(_ => {
                    var section = configuration.GetSection(configSectionName);
                    var options = new S3FileStorageOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }

            // Register IAmazonS3 if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonS3))) {
                services.AddSingleton<IAmazonS3>(provider => {
                    var options = provider.GetRequiredService<S3FileStorageOptions>();
                    var config = new AmazonS3Config();
                    if (!string.IsNullOrWhiteSpace(options.Region)) {
                        var region = RegionEndpoint.GetBySystemName(options.Region);
                        config.RegionEndpoint = region;
                    }

                    if (!string.IsNullOrWhiteSpace(options.ServiceUrl)) {
                        config.ServiceURL = options.ServiceUrl;
                        config.ForcePathStyle = true; // Required for S3-compatible services
                    }

                    if (!string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey)) {
                        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
                        // Explicitly create client with credentials to avoid default credential chain
                        var client = new AmazonS3Client(credentials, config);
                        return client;
                    }

                    // If no credentials provided, use default credential chain
                    return new AmazonS3Client(config);
                });
            }

            return services;
        }
    }
}