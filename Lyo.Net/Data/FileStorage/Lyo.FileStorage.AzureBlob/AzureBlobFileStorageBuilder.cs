using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.AzureBlob.Multipart;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.AzureBlob;

/// <summary>Inline and section configuration for one <see cref="AzureBlobFileStorageService" />. Call <see cref="WithMultipart" /> to register multipart for that store.</summary>
public sealed class AzureBlobFileStorageBuilder : FileStorageServiceBuilder<AzureBlobFileStorageOptions, AzureBlobFileStorageBuilder>
{
    private bool _multipart;

    /// <summary>Registers keyed multipart that closes over this store. The short <c>AddAzureBlob</c> form does not call this.</summary>
    public AzureBlobFileStorageBuilder WithMultipart()
    {
        _multipart = true;
        return this;
    }

    /// <inheritdoc />
    protected override void Bind(IConfiguration section) => section.Bind(Options);

    /// <inheritdoc />
    protected override void Validate() => Options.Validate();

    /// <inheritdoc />
    public override void RegisterFactoryServices(IServiceCollection services, string storeName)
    {
        services.AddAzureBlobFileStorageProvider();
        if (!_multipart)
            return;

        services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
        services.AddKeyedScoped<AzureBlobMultipartUploadService>(storeName, (provider, _) => {
            if (provider.GetRequiredService<IFileStorageFactory>().Get(storeName) is not AzureBlobFileStorageService storage)
                throw new InvalidOperationException($"File storage '{storeName}' is not an Azure Blob store.");

            var metrics = Options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            return new AzureBlobMultipartUploadService(
                storage, Options, provider.GetRequiredService<IMultipartUploadSessionStore>(), provider.GetService<IFileContentPolicy>(),
                provider.GetServices<IFileAuditEventHandler>(), provider.GetService<IFileOperationContextAccessor>(), provider.GetService<ILoggerFactory>(), metrics);
        });
        services.AddKeyedScoped<IMultipartUploadService>(storeName, (provider, _) => provider.GetRequiredKeyedService<AzureBlobMultipartUploadService>(storeName));
    }

    /// <summary>Registers unkeyed multipart when <see cref="WithMultipart" /> was called. Used by the single-store overload.</summary>
    public void RegisterUnkeyedMultipart(IServiceCollection services)
    {
        if (!_multipart)
            return;

        services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
        services.AddScoped<AzureBlobMultipartUploadService>(provider => {
            if (provider.GetRequiredService<IFileStorageService>() is not AzureBlobFileStorageService storage)
                throw new InvalidOperationException("The unkeyed file storage service is not an Azure Blob store.");

            var metrics = Options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            return new AzureBlobMultipartUploadService(
                storage, Options, provider.GetRequiredService<IMultipartUploadSessionStore>(), provider.GetService<IFileContentPolicy>(),
                provider.GetServices<IFileAuditEventHandler>(), provider.GetService<IFileOperationContextAccessor>(), provider.GetService<ILoggerFactory>(), metrics);
        });
        services.AddScoped<IMultipartUploadService>(provider => provider.GetRequiredService<AzureBlobMultipartUploadService>());
    }

    /// <inheritdoc />
    protected override IFileStorageService Create(IServiceProvider services, string storeName)
    {
        var metrics = Options.EnableMetrics ? services.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        return new AzureBlobFileStorageService(
            Options, ResolveRequiredMetadata(services, storeName), services.GetService<ILoggerFactory>(), ResolveCompression(services, storeName),
            ResolveEncryption(services, storeName), null, metrics, services.GetService<IFileOperationContextAccessor>(), services.GetServices<IFileAuditEventHandler>(),
            services.GetService<IFileContentPolicy>(), services.GetService<IFileTypeSniffer>());
    }
}
