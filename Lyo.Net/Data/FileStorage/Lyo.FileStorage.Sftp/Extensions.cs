using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Lyo.Sftp.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.Sftp;

/// <summary>DI helpers that register SFTP-backed file storage.</summary>
public static class Extensions
{
    private static void RegisterService(IServiceCollection services)
    {
        global::Lyo.FileStorage.FileStorageServiceRegistration.AddScopedFileStorage<SftpFileStorageService>(services, sp => {
            var opts = sp.GetRequiredService<SftpFileStorageOptions>();
            var metadataStore = sp.GetRequiredService<IFileMetadataStore>();
            var sftp = sp.GetRequiredService<ISftpClient>();
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var compression = sp.GetService<ICompressionService>();
            var encryption = sp.GetService<ITwoKeyEncryptionService>();
            var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            var op = sp.GetService<IFileOperationContextAccessor>();
            var auditHandlers = sp.GetServices<IFileAuditEventHandler>();
            var policy = sp.GetService<IFileContentPolicy>();
            return new(opts, metadataStore, sftp, loggerFactory, compression, encryption, metrics, op, auditHandlers, policy);
        });
    }

    extension(IServiceCollection services)
    {
        /// <summary>Registers SFTP-backed file storage and an <see cref="ISftpClient" /> from <see cref="SftpFileStorageOptions.Sftp" />.</summary>
        public IServiceCollection AddSftpFileStorageService(SftpFileStorageOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddSftpClient(options.Sftp);
            RegisterService(services);
            return services;
        }

        /// <summary>Registers SFTP-backed file storage and runs <paramref name="configure" /> on the options.</summary>
        public IServiceCollection AddSftpFileStorageService(Action<SftpFileStorageOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SftpFileStorageOptions();
            configure(options);
            return services.AddSftpFileStorageService(options);
        }

        /// <summary>Binds <see cref="SftpFileStorageOptions" /> from configuration and registers SFTP-backed file storage.</summary>
        public IServiceCollection AddSftpFileStorageServiceFromConfiguration(IConfiguration configuration, string sectionName = SftpFileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new SftpFileStorageOptions();
            configuration.GetSection(sectionName).Bind(options);

            return services.AddSftpFileStorageService(options);
        }
    }
}