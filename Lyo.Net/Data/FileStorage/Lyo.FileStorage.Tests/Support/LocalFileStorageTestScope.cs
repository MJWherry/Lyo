using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage.Tests.Support;

/// <summary>
/// Disposable scope that creates a one-shot temp root directory and a <see cref="LocalFileStorageService" /> bound to it, then cleans the directory up on dispose. Replaces
/// the ad-hoc <c>Directory.CreateDirectory</c> + <c>try/finally</c> blocks that were duplicated across every test file.
/// </summary>
public sealed class LocalFileStorageTestScope : IDisposable
{
    private readonly string _root;

    public DiskFileStorageOptions Options { get; }

    public LocalFileStorageService Storage { get; }

    private LocalFileStorageTestScope(string root, DiskFileStorageOptions options, LocalFileStorageService storage)
    {
        _root = root;
        Options = options;
        Storage = storage;
    }

    public void Dispose()
    {
        try {
            if (Storage is IDisposable d)
                d.Dispose();
        }
        catch {
            // best-effort
        }

        try {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch {
            // best-effort
        }
    }

    public static LocalFileStorageTestScope Create(
        Func<DiskFileStorageOptions, DiskFileStorageOptions>? builder = null,
        IEnumerable<IFileAuditEventHandler>? auditHandlers = null,
        IFileContentPolicy? contentPolicy = null,
        IFileMalwareScanner? malwareScanner = null,
        IFileMetadataStore? metadataService = null,
        IMetrics? metrics = null,
        IFileOperationContextAccessor? operationContextAccessor = null,
        ILoggerFactory? loggerFactory = null,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fs-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var options = new DiskFileStorageOptions { RootDirectoryPath = root };
        if (builder != null)
            options = builder(options);

        var storage = new LocalFileStorageService(
            options, loggerFactory ?? NullLoggerFactory.Instance, compressionService, twoKeyEncryptionService: twoKeyEncryptionService, metrics: metrics,
            operationContextAccessor: operationContextAccessor, auditHandlers: auditHandlers, contentPolicy: contentPolicy, malwareScanner: malwareScanner,
            metadataService: metadataService);

        return new(root, options, storage);
    }
}