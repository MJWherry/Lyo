using Amazon;
using Amazon.S3;
using Lyo.FileStorage.Abstractions;
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

/// <summary>Inline and section configuration for one <see cref="S3FileStorageService" />. Multipart closes over this store's options, client, and service.</summary>
public sealed class S3FileStorageBuilder : FileStorageServiceBuilder<S3FileStorageOptions, S3FileStorageBuilder>
{
    private readonly object _clientGate = new();
    private IAmazonS3? _client;

    /// <inheritdoc />
    protected override void Bind(IConfiguration section) => section.Bind(Options);

    /// <inheritdoc />
    protected override void Validate() => Options.Validate();

    /// <inheritdoc />
    public override void RegisterFactoryServices(IServiceCollection services, string storeName)
    {
        services.AddS3FileStorageProvider();
        services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
        services.AddKeyedScoped<S3MultipartUploadService>(storeName, (provider, _) => {
            if (provider.GetRequiredService<IFileStorageFactory>().Get(storeName) is not S3FileStorageService storage)
                throw new InvalidOperationException($"File storage '{storeName}' is not an S3 store.");

            var metrics = Options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            return new S3MultipartUploadService(
                storage, Options, Client(), provider.GetRequiredService<IMultipartUploadSessionStore>(), provider.GetService<IFileContentPolicy>(),
                provider.GetServices<IFileAuditEventHandler>(), provider.GetService<IFileOperationContextAccessor>(), provider.GetService<ILoggerFactory>(), metrics);
        });
        services.AddKeyedScoped<IMultipartUploadService>(storeName, (provider, _) => provider.GetRequiredKeyedService<S3MultipartUploadService>(storeName));
    }

    /// <summary>Registers unkeyed multipart for <see cref="S3FileStorageFactoryExtensions.AddS3FileStorage" />. It does not call <c>AddKeyedS3MultipartUploadService</c>.</summary>
    public void RegisterUnkeyedMultipart(IServiceCollection services)
    {
        services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
        services.AddScoped<S3MultipartUploadService>(provider => {
            if (provider.GetRequiredService<IFileStorageService>() is not S3FileStorageService storage)
                throw new InvalidOperationException("The unkeyed file storage service is not an S3 store.");

            var metrics = Options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            return new S3MultipartUploadService(
                storage, Options, Client(), provider.GetRequiredService<IMultipartUploadSessionStore>(), provider.GetService<IFileContentPolicy>(),
                provider.GetServices<IFileAuditEventHandler>(), provider.GetService<IFileOperationContextAccessor>(), provider.GetService<ILoggerFactory>(), metrics);
        });
        services.AddScoped<IMultipartUploadService>(provider => provider.GetRequiredService<S3MultipartUploadService>());
    }

    /// <inheritdoc />
    protected override IFileStorageService Create(IServiceProvider services, string storeName)
    {
        var metrics = Options.EnableMetrics ? services.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        return new S3FileStorageService(
            Options, ResolveRequiredMetadata(services, storeName), services.GetService<ILoggerFactory>(), ResolveCompression(services, storeName),
            ResolveEncryption(services, storeName), Client(), metrics, services.GetService<IFileOperationContextAccessor>(), services.GetServices<IFileAuditEventHandler>(),
            services.GetService<IFileContentPolicy>(), services.GetService<IFileTypeSniffer>());
    }

    private IAmazonS3 Client()
    {
        if (_client != null)
            return _client;

        lock (_clientGate) {
            if (_client != null)
                return _client;

            var config = new AmazonS3Config();
            if (!string.IsNullOrWhiteSpace(Options.Region))
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(Options.Region);

            if (!string.IsNullOrWhiteSpace(Options.ServiceUrl)) {
                config.ServiceURL = Options.ServiceUrl;
                config.ForcePathStyle = true;
            }

            var credentials = S3AwsCredentialHelpers.Resolve(Options.AccessKeyId, Options.SecretAccessKey, Options.Profile);
            _client = credentials is null ? new AmazonS3Client(config) : new AmazonS3Client(credentials, config);
            return _client;
        }
    }
}
