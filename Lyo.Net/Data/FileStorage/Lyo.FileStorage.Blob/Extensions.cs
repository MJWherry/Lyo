using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Blob.Multipart;
using Lyo.FileStorage.Blob.Staged;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Blob;

public static class Extensions
{
    private static void RegisterBlobService(IServiceCollection services)
    {
        services.AddScoped<BlobFileStorageService>(sp => {
            var opts = sp.GetRequiredService<BlobFileStorageOptions>();
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

        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<BlobFileStorageService>());
    }

    extension(IServiceCollection services)
    {
        /// <summary>Adds Azure Blob–backed <see cref="BlobFileStorageService" />.</summary>
        public IServiceCollection AddBlobFileStorageService(BlobFileStorageOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            RegisterBlobService(services);
            return services;
        }

        /// <summary>Binds from configuration (primary <see cref="BlobFileStorageOptions.SectionName" />, else legacy <c>AzureFileStorageOptions</c>).</summary>
        public IServiceCollection AddBlobFileStorageService(string configSectionName = BlobFileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddSingleton(provider => {
                var config = provider.GetRequiredService<IConfiguration>();
                var options = new BlobFileStorageOptions();
                var section = config.GetSection(configSectionName);
                if (section.Exists())
                    section.Bind(options);
                else {
                    var legacy = config.GetSection(BlobFileStorageOptions.LegacyAzureConfigurationSectionName);
                    if (!legacy.Exists())
                        return options;

                    legacy.Bind(options);
                    (provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance).CreateLogger("Lyo.FileStorage.Blob")
                        .LogWarning(
                            "Loaded blob file storage configuration from obsolete section [{Legacy}]. Migrate to [{Current}] in appsettings.",
                            BlobFileStorageOptions.LegacyAzureConfigurationSectionName, BlobFileStorageOptions.SectionName);
                }

                return options;
            });

            RegisterBlobService(services);
            return services;
        }

        /// <summary>Registers block blob multipart uploads (same flow as multipart on S3).</summary>
        public IServiceCollection AddBlobMultipartUploadService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
            services.AddScoped<BlobMultipartUploadService>(sp => {
                var opts = sp.GetRequiredService<BlobFileStorageOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new(
                    sp.GetRequiredService<BlobFileStorageService>(), opts, sp.GetRequiredService<IMultipartUploadSessionStore>(), sp.GetService<IFileMalwareScanner>(),
                    sp.GetService<IFileContentPolicy>(), sp.GetServices<IFileAuditEventHandler>(), sp.GetService<IFileOperationContextAccessor>(), sp.GetService<ILoggerFactory>(),
                    metrics);
            });

            services.AddScoped<IMultipartUploadService>(sp => sp.GetRequiredService<BlobMultipartUploadService>());
            return services;
        }

        /// <summary>Registers staged uploads with Azure Blob SAS PUT URLs.</summary>
        public IServiceCollection AddBlobStagedFileUploadService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddInMemoryStagedFileUploadStoreIfMissing();
            services.AddScoped<BlobStagedFileUploadService>(sp => {
                var opts = sp.GetRequiredService<BlobFileStorageOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new(
                    sp.GetRequiredService<BlobFileStorageService>(),
                    opts,
                    sp.GetRequiredService<IStagedFileUploadStore>(),
                    null,
                    sp.GetService<IFileMalwareScanner>(),
                    sp.GetService<IFileContentPolicy>(),
                    sp.GetServices<IFileAuditEventHandler>(),
                    sp.GetServices<IStagedFileUploadEventHandler>(),
                    sp.GetService<IFileOperationContextAccessor>(),
                    sp.GetService<ILoggerFactory>(),
                    metrics);
            });

            services.AddScoped<IStagedFileUploadService>(sp => sp.GetRequiredService<BlobStagedFileUploadService>());
            return services;
        }
    }
}