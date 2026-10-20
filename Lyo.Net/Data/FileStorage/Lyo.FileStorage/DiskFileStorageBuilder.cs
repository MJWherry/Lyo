using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage;

/// <summary>Inline and section configuration for one <see cref="LocalFileStorageService" />.</summary>
public sealed class DiskFileStorageBuilder : FileStorageServiceBuilder<DiskFileStorageOptions, DiskFileStorageBuilder>
{
    /// <inheritdoc />
    protected override void Bind(IConfiguration section) => section.Bind(Options);

    /// <inheritdoc />
    protected override void Validate() => Options.Validate();

    /// <inheritdoc />
    protected override IFileStorageService Create(IServiceProvider services, string storeName)
    {
        var metrics = Options.EnableMetrics ? services.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        return new LocalFileStorageService(
            Options, services.GetService<ILoggerFactory>(), ResolveCompression(services, storeName), ResolveEncryption(services, storeName),
            ResolveMetadata(services, storeName), metrics, services.GetService<OperationContext.IFileOperationContextAccessor>(),
            services.GetServices<Audit.IFileAuditEventHandler>(), services.GetService<Policy.IFileContentPolicy>(), services.GetService<Policy.IFileTypeSniffer>());
    }
}
