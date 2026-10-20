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

/// <summary>Inline and section configuration for one <see cref="SftpFileStorageService" />. The SFTP client is constructed inside the store delegate.</summary>
public sealed class SftpFileStorageBuilder : FileStorageServiceBuilder<SftpFileStorageOptions, SftpFileStorageBuilder>
{
    /// <inheritdoc />
    protected override void Bind(IConfiguration section) => section.Bind(Options);

    /// <inheritdoc />
    protected override void Validate() => Options.Validate();

    /// <inheritdoc />
    public override void RegisterFactoryServices(IServiceCollection services, string storeName) => services.AddSftpFileStorageProvider();

    /// <inheritdoc />
    protected override IFileStorageService Create(IServiceProvider services, string storeName)
    {
        var metrics = Options.EnableMetrics ? services.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        var loggerFactory = services.GetService<ILoggerFactory>();
        var client = new SftpClient(Options.Sftp, loggerFactory, metrics);
        return new SftpFileStorageService(
            Options, ResolveRequiredMetadata(services, storeName), client, loggerFactory, ResolveCompression(services, storeName), ResolveEncryption(services, storeName),
            metrics, services.GetService<IFileOperationContextAccessor>(), services.GetServices<IFileAuditEventHandler>(), services.GetService<IFileContentPolicy>(),
            services.GetService<IFileTypeSniffer>(), ownsClient: true);
    }
}
