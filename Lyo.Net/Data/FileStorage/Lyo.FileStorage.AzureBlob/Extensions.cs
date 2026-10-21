using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
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

public static class Extensions
{
    private static void RegisterBlobService(IServiceCollection services)
        => FileStorageServiceRegistration.AddScopedFileStorage<AzureBlobFileStorageService>(
            services, sp => {
                var opts = sp.GetRequiredService<AzureBlobFileStorageOptions>();
                var metadataStore = sp.GetRequiredService<IFileMetadataStore>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var compression = sp.GetService<ICompressionService>();
                var encryption = sp.GetService<ITwoKeyEncryptionService>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var op = sp.GetService<IFileOperationContextAccessor>();
                var auditHandlers = sp.GetServices<IFileAuditEventHandler>();
                var policy = sp.GetService<IFileContentPolicy>();
                return new(opts, metadataStore, loggerFactory, compression, encryption, null, metrics, op, auditHandlers, policy, sp.GetService<IFileTypeSniffer>());
            });

    extension(IServiceCollection services)
    {
        /// <summary>Adds Azure Blob–backed <see cref="AzureBlobFileStorageService" /> with the given options instance.</summary>
        public IServiceCollection AddAzureBlobFileStorageService(AzureBlobFileStorageOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            RegisterBlobService(services);
            return services;
        }

        /// <summary>Binds <see cref="AzureBlobFileStorageOptions" /> from configuration (same shape as S3/Ftp/Sftp <c>FromConfiguration</c>).</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration).</param>
        /// <param name="sectionName">Section to bind; defaults to <see cref="AzureBlobFileStorageOptions.SectionName" />.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddAzureBlobFileStorageServiceFromConfiguration(IConfiguration configuration, string sectionName = AzureBlobFileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new AzureBlobFileStorageOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddAzureBlobFileStorageService(options);
        }

        /// <summary>Binds <see cref="AzureBlobFileStorageOptions" /> from <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddAzureBlobFileStorageService(string configSectionName = AzureBlobFileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddSingleton(provider => {
                var config = provider.GetRequiredService<IConfiguration>();
                var options = new AzureBlobFileStorageOptions();
                config.GetSection(configSectionName).Bind(options);
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
                    sp.GetRequiredService<AzureBlobFileStorageService>(), opts, sp.GetRequiredService<IMultipartUploadSessionStore>(), sp.GetService<IFileContentPolicy>(),
                    sp.GetServices<IFileAuditEventHandler>(), sp.GetService<IFileOperationContextAccessor>(), sp.GetService<ILoggerFactory>(), metrics);
            });

            services.AddScoped<IMultipartUploadService>(sp => sp.GetRequiredService<AzureBlobMultipartUploadService>());
            return services;
        }
    }
}