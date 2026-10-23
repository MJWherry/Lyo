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

/// <summary>Inline and section configuration for one <see cref="FtpFileStorageService" />. The FTP client is constructed inside the store delegate.</summary>
public sealed class FtpFileStorageBuilder : FileStorageServiceBuilder<FtpFileStorageOptions, FtpFileStorageBuilder>
{
    /// <inheritdoc />
    protected override void Bind(IConfiguration section) => section.Bind(Options);

    /// <inheritdoc />
    protected override void Validate() => Options.Validate();

    /// <inheritdoc />
    public override void RegisterFactoryServices(IServiceCollection services, string storeName) => services.AddFtpFileStorageProvider();

    /// <inheritdoc />
    protected override IFileStorageService Create(IServiceProvider services, string storeName)
    {
        var metrics = Options.EnableMetrics ? services.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        var loggerFactory = services.GetService<ILoggerFactory>();
        var client = new FtpClient(Options.Ftp, loggerFactory, metrics);
        return new FtpFileStorageService(
            Options, ResolveRequiredMetadata(services, storeName), client, loggerFactory, ResolveCompression(services, storeName), ResolveEncryption(services, storeName), metrics,
            services.GetService<IFileOperationContextAccessor>(), services.GetServices<IFileAuditEventHandler>(), services.GetService<IFileContentPolicy>(),
            services.GetService<IFileTypeSniffer>(), true);
    }
}