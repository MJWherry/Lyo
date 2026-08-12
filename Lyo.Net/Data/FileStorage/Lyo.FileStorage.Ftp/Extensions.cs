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

/// <summary>DI registration for FTP file storage.</summary>
public static class Extensions
{
    private static void RegisterService(IServiceCollection services)
    {
        services.AddScoped<FtpFileStorageService>(sp => {
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
            var scan = sp.GetService<IFileMalwareScanner>();
            return new(opts, metadataStore, ftp, loggerFactory, compression, encryption, metrics, op, auditHandlers, policy, scan);
        });

        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<FtpFileStorageService>());
    }

    extension(IServiceCollection services)
    {
        /// <summary>Adds FTP-backed file storage and registers <see cref="IFtpClient" /> from <see cref="FtpFileStorageOptions.Ftp" />.</summary>
        public IServiceCollection AddFtpFileStorageService(FtpFileStorageOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Ftp.Validate();
            services.AddSingleton(options);
            services.AddFtpClient(options.Ftp);
            RegisterService(services);
            return services;
        }

        /// <summary>Adds FTP-backed file storage using a configure callback.</summary>
        public IServiceCollection AddFtpFileStorageService(Action<FtpFileStorageOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new FtpFileStorageOptions();
            configure(options);
            return services.AddFtpFileStorageService(options);
        }

        /// <summary>Binds <see cref="FtpFileStorageOptions" /> from configuration and registers FTP file storage.</summary>
        public IServiceCollection AddFtpFileStorageServiceFromConfiguration(IConfiguration configuration, string sectionName = FtpFileStorageOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new FtpFileStorageOptions();
            var section = configuration.GetSection(sectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddFtpFileStorageService(options);
        }
    }
}