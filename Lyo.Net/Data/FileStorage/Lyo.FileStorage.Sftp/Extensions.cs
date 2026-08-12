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

/// <summary>DI registration for SFTP file storage.</summary>
public static class Extensions
{
    private static void RegisterService(IServiceCollection services)
    {
        services.AddScoped<SftpFileStorageService>(sp => {
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
            var scan = sp.GetService<IFileMalwareScanner>();
            return new(opts, metadataStore, sftp, loggerFactory, compression, encryption, metrics, op, auditHandlers, policy, scan);
        });
        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<SftpFileStorageService>());
    }

    extension(IServiceCollection services)
    {
        /// <summary>Adds SFTP-backed file storage and registers <see cref="ISftpClient" /> from <see cref="SftpFileStorageOptions.Sftp" />.</summary>
        public IServiceCollection AddSftpFileStorageService(SftpFileStorageOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Sftp.Validate();
            services.AddSingleton(options);
            services.AddSftpClient(options.Sftp);
            RegisterService(services);
            return services;
        }

        /// <summary>Adds SFTP-backed file storage using a configure callback.</summary>
        public IServiceCollection AddSftpFileStorageService(Action<SftpFileStorageOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SftpFileStorageOptions();
            configure(options);
            return services.AddSftpFileStorageService(options);
        }

        /// <summary>Binds <see cref="SftpFileStorageOptions" /> from configuration and registers SFTP file storage.</summary>
        public IServiceCollection AddSftpFileStorageServiceFromConfiguration(IConfiguration configuration, string sectionName = SftpFileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new SftpFileStorageOptions();
            var section = configuration.GetSection(sectionName);
            if (section.Exists())
                section.Bind(options);
            return services.AddSftpFileStorageService(options);
        }
    }
}
