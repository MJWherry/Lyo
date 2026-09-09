using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Remote;
using Lyo.Ftp.Client;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Ftp;

/// <summary>FTP-backed <see cref="IFileStorageService" />. Physical IO only. Presigned and multipart are not supported.</summary>
public sealed class FtpFileStorageService : RemoteFileStorageServiceBase
{
    /// <summary>Builds an FTP file storage service.</summary>
    public FtpFileStorageService(
        FtpFileStorageOptions options,
        IFileMetadataStore metadataService,
        IFtpClient ftpClient,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null)
        : base(
            options, metadataService, new FtpRemoteFileTransport(ArgumentHelpers.ThrowIfNullReturn(ftpClient)),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<FtpFileStorageService>(), compressionService, twoKeyEncryptionService, metrics, operationContextAccessor,
            auditHandlers, contentPolicy) { }
}
