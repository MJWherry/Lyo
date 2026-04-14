using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Azure.Multipart;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.Azure;

public static class Extensions
{
    /// <summary>Adds Azure Blob file storage service to the service collection. Requires IFileMetadataStore to be registered.</summary>
    public static IServiceCollection AddAzureFileStorageService(this IServiceCollection services, AzureFileStorageOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddScoped<AzureFileStorageService>(sp => {
            var opts = sp.GetRequiredService<AzureFileStorageOptions>();
            var metadataStore = sp.GetRequiredService<IFileMetadataStore>();
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var compression = sp.GetService<ICompressionService>();
            var encryption = sp.GetService<ITwoKeyEncryptionService>();
            var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            var op = sp.GetService<IFileOperationContextAccessor>();
            var auditHandlers = sp.GetServices<IFileAuditEventHandler>();
            var policy = sp.GetService<IFileContentPolicy>();
            var scan = sp.GetService<IFileMalwareScanner>();
            return new(opts, metadataStore, loggerFactory, compression, encryption, null, metrics, op, auditHandlers, policy, scan);
        });

        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<AzureFileStorageService>());
        return services;
    }

    /// <summary>Adds Azure Blob file storage service from configuration.</summary>
    public static IServiceCollection AddAzureFileStorageService(this IServiceCollection services, string configSectionName = AzureFileStorageOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        services.AddSingleton<AzureFileStorageOptions>(sp => {
            var config = sp.GetRequiredService<IConfiguration>();
            var section = config.GetSection(configSectionName);
            var options = new AzureFileStorageOptions();
            if (section.Exists())
                section.Bind(options);

            return options;
        });

        services.AddScoped<AzureFileStorageService>(sp => {
            var opts = sp.GetRequiredService<AzureFileStorageOptions>();
            var metadataStore = sp.GetRequiredService<IFileMetadataStore>();
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var compression = sp.GetService<ICompressionService>();
            var encryption = sp.GetService<ITwoKeyEncryptionService>();
            var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            var op = sp.GetService<IFileOperationContextAccessor>();
            var auditHandlers = sp.GetServices<IFileAuditEventHandler>();
            var policy = sp.GetService<IFileContentPolicy>();
            var scan = sp.GetService<IFileMalwareScanner>();
            return new(opts, metadataStore, loggerFactory, compression, encryption, null, metrics, op, auditHandlers, policy, scan);
        });

        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<AzureFileStorageService>());
        return services;
    }

    /// <summary>Registers <see cref="AzureMultipartUploadService" /> for S3-style multipart flows using block blob staging.</summary>
    public static IServiceCollection AddAzureMultipartUploadService(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
        services.AddScoped<AzureMultipartUploadService>(sp => {
            var opts = sp.GetRequiredService<AzureFileStorageOptions>();
            var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            return new(
                sp.GetRequiredService<AzureFileStorageService>(), opts, sp.GetRequiredService<IMultipartUploadSessionStore>(), sp.GetService<IFileMalwareScanner>(),
                sp.GetServices<IFileAuditEventHandler>(), sp.GetService<IFileOperationContextAccessor>(), sp.GetService<ILoggerFactory>(), metrics);
        });

        services.AddScoped<IMultipartUploadService>(sp => sp.GetRequiredService<AzureMultipartUploadService>());
        return services;
    }
}