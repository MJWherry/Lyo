using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.AzureBlob.Multipart;
using Lyo.FileStorage.AzureBlob.Staged;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Staged;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.AzureBlob;

public static class Extensions
{
    private static void RegisterBlobService(IServiceCollection services)
    {
        services.AddScoped<AzureBlobFileStorageService>(sp => {
            var opts = sp.GetRequiredService<AzureBlobFileStorageOptions>();
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

        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<AzureBlobFileStorageService>());
    }

    extension(IServiceCollection services)
    {
        /// <summary>Adds Azure Blob–backed <see cref="AzureBlobFileStorageService" />.</summary>
        public IServiceCollection AddAzureBlobFileStorageService(AzureBlobFileStorageOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            RegisterBlobService(services);
            return services;
        }

        /// <summary>Binds from configuration (primary <see cref="AzureBlobFileStorageOptions.SectionName" />, else legacy <c>BlobFileStorage</c> / <c>AzureFileStorageOptions</c>).</summary>
        public IServiceCollection AddAzureBlobFileStorageService(string configSectionName = AzureBlobFileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddSingleton(provider => {
                var config = provider.GetRequiredService<IConfiguration>();
                var options = new AzureBlobFileStorageOptions();
                var section = config.GetSection(configSectionName);
                if (section.Exists())
                    section.Bind(options);
                else {
                    var legacyName = AzureBlobFileStorageOptions.LegacyBlobConfigurationSectionName;
                    var legacy = config.GetSection(legacyName);
                    if (!legacy.Exists()) {
                        legacyName = AzureBlobFileStorageOptions.LegacyAzureConfigurationSectionName;
                        legacy = config.GetSection(legacyName);
                    }

                    if (!legacy.Exists())
                        return options;

                    legacy.Bind(options);
                    (provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance).CreateLogger("Lyo.FileStorage.AzureBlob")
                        .LogWarning(
                            "Loaded blob file storage configuration from obsolete section [{Legacy}]. Migrate to [{Current}] in appsettings.",
                            legacyName, AzureBlobFileStorageOptions.SectionName);
                }

                return options;
            });

            RegisterBlobService(services);
            return services;
        }

        /// <summary>Registers block blob multipart uploads (same flow as multipart on S3).</summary>
        public IServiceCollection AddAzureBlobMultipartUploadService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
            services.AddScoped<AzureBlobMultipartUploadService>(sp => {
                var opts = sp.GetRequiredService<AzureBlobFileStorageOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new(
                    sp.GetRequiredService<AzureBlobFileStorageService>(), opts, sp.GetRequiredService<IMultipartUploadSessionStore>(), sp.GetService<IFileMalwareScanner>(),
                    sp.GetService<IFileContentPolicy>(), sp.GetServices<IFileAuditEventHandler>(), sp.GetService<IFileOperationContextAccessor>(), sp.GetService<ILoggerFactory>(),
                    metrics);
            });

            services.AddScoped<IMultipartUploadService>(sp => sp.GetRequiredService<AzureBlobMultipartUploadService>());
            return services;
        }

        /// <summary>Registers staged uploads with Azure Blob SAS PUT URLs.</summary>
        public IServiceCollection AddAzureBlobStagedFileUploadService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddInMemoryStagedFileUploadStoreIfMissing();
            services.AddScoped<AzureBlobStagedFileUploadService>(sp => {
                var opts = sp.GetRequiredService<AzureBlobFileStorageOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                return new(
                    sp.GetRequiredService<AzureBlobFileStorageService>(), opts, sp.GetRequiredService<IStagedFileUploadStore>(), null, sp.GetService<IFileMalwareScanner>(),
                    sp.GetService<IFileContentPolicy>(), sp.GetServices<IFileAuditEventHandler>(), sp.GetServices<IStagedFileUploadEventHandler>(),
                    sp.GetService<IFileOperationContextAccessor>(), sp.GetService<ILoggerFactory>(), metrics);
            });

            services.AddScoped<IStagedFileUploadService>(sp => sp.GetRequiredService<AzureBlobStagedFileUploadService>());
            return services;
        }
    }
}