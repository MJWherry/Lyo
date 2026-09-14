using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Ftp.Client;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.Ftp;

/// <summary>DI helpers that register FTP-backed file storage.</summary>
public static class Extensions
{
    private static void RegisterService(IServiceCollection services)
    {
        global::Lyo.FileStorage.FileStorageServiceRegistration.AddScopedFileStorage<FtpFileStorageService>(services, sp => {
            var opts = sp.GetRequiredService<FtpFileStorageOptions>();
            var metadataStore = sp.GetRequiredService<IFileMetadataStore>();
            var ftp = sp.GetRequiredService<IFtpClient>();
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var compression = sp.GetService<ICompressionService>();
            var encryption = sp.GetService<ITwoKeyEncryptionService>();
            var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
            var op = sp.GetService<IFileOperationContextAccessor>();
            var auditHandlers = sp.GetServices<IFileAuditEventHandler>();
            var policy = sp.GetService<IFileContentPolicy>();
            return new(opts, metadataStore, ftp, loggerFactory, compression, encryption, metrics, op, auditHandlers, policy);
        });
    }

    extension(IServiceCollection services)
    {
        /// <summary>Registers FTP-backed file storage and an <see cref="IFtpClient" /> from <see cref="FtpFileStorageOptions.Ftp" />.</summary>
        public IServiceCollection AddFtpFileStorageService(FtpFileStorageOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddFtpClient(options.Ftp);
            RegisterService(services);
            return services;
        }

        /// <summary>Registers FTP-backed file storage and runs <paramref name="configure" /> on the options.</summary>
        public IServiceCollection AddFtpFileStorageService(Action<FtpFileStorageOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new FtpFileStorageOptions();
            configure(options);
            return services.AddFtpFileStorageService(options);
        }

        /// <summary>Binds <see cref="FtpFileStorageOptions" /> from configuration and registers FTP-backed file storage.</summary>
        public IServiceCollection AddFtpFileStorageServiceFromConfiguration(IConfiguration configuration, string sectionName = FtpFileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new FtpFileStorageOptions();
            configuration.GetSection(sectionName).Bind(options);

            return services.AddFtpFileStorageService(options);
        }
    }
}