using Amazon;
using Amazon.S3;
using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.S3.Multipart;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.S3;

public static class Extensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="S3MultipartUploadService" /> as a keyed <see cref="IMultipartUploadService" /> (same key as keyed <see cref="S3FileStorageService" />). Typically
        /// invoked by <see cref="S3FileStorageServiceBuilder.Build" />; call this only to register multipart alone or to replace the default registration.
        /// </summary>
        public IServiceCollection AddKeyedS3MultipartUploadService(string serviceKey)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(serviceKey);
            services.AddKeyedScoped<S3MultipartUploadService>(
                serviceKey, (provider, _) => {
                    var opts = provider.GetRequiredService<S3FileStorageOptions>();
                    var metrics = opts.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                    return new(
                        provider.GetRequiredKeyedService<S3FileStorageService>(serviceKey), opts, provider.GetRequiredService<IAmazonS3>(),
                        provider.GetRequiredService<IMultipartUploadSessionStore>(), provider.GetService<IFileContentPolicy>(),
                        provider.GetServices<IFileAuditEventHandler>(), provider.GetService<IFileOperationContextAccessor>(), provider.GetService<ILoggerFactory>(), metrics);
                });

            services.AddKeyedScoped<IMultipartUploadService>(serviceKey, (provider, _) => provider.GetRequiredKeyedService<S3MultipartUploadService>(serviceKey));
            return services;
        }

        /// <summary>
        /// Alias for <see cref="AddKeyedS3MultipartUploadService" /> for callers who think in terms of the AWS S3 API (works for any S3-compatible endpoint: AWS, MinIO,
        /// Wasabi, R2, etc.).
        /// </summary>
        public IServiceCollection AddKeyedAwsMultipartUploadService(string serviceKey) => services.AddKeyedS3MultipartUploadService(serviceKey);

        /// <summary>Adds a keyed S3 file storage service (AWS, Backblaze B2, MinIO, etc.) and returns a builder for further setup.</summary>
        /// <param name="keyName">DI key.</param>
        /// <returns>Builder for configuring the service and its dependencies.</returns>
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
        public S3FileStorageServiceBuilder AddS3FileStorageServiceKeyed(string keyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            return new(services, keyName);
        }

        /// <summary>Registers IAmazonS3 from configuration. Binds <see cref="S3FileStorageOptions" /> from the specified configuration section.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">Section name; starts as <see cref="S3FileStorageOptions.SectionName" />.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAmazonS3FromConfiguration(IConfiguration configuration, string configSectionName = S3FileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(S3FileStorageOptions))) {
                services.AddSingleton<S3FileStorageOptions>(_ => {
                    var options = LyoOptions.Bind<S3FileStorageOptions>(configuration, configSectionName);
                    options.Validate();

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

                    var credentials = S3AwsCredentialHelpers.Resolve(options.AccessKeyId, options.SecretAccessKey, options.Profile);
                    return credentials is null ? new AmazonS3Client(config) : new AmazonS3Client(credentials, config);
                });
            }

            return services;
        }
    }
}