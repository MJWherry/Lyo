using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Remote;
using Lyo.Metrics;
using Lyo.Sftp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Sftp;

/// <summary>SFTP-backed <see cref="IFileStorageService" />. Physical IO only. Presigned and multipart are not supported.</summary>
public sealed class SftpFileStorageService : RemoteFileStorageServiceBase
{
    /// <summary>Builds an SFTP file storage service.</summary>
    public SftpFileStorageService(
        SftpFileStorageOptions options,
        IFileMetadataStore metadataService,
        ISftpClient sftpClient,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null)
        : base(
            options, metadataService, new SftpRemoteFileTransport(ArgumentHelpers.ThrowIfNullReturn(sftpClient)),
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SftpFileStorageService>(), compressionService, twoKeyEncryptionService, metrics, operationContextAccessor,
            auditHandlers, contentPolicy) { }
}
