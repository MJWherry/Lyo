using System.Text.Json;
using Lyo.Compression.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Models;
using Lyo.Health;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.FileStorage;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Api.Tests;

public sealed class ScopedPlanFileStorageTests
{
    [Fact]
    public async Task SaveSnapshot_ScopedService_ResolvesInsideAScope()
    {
        var runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var stepId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var (storage, memory, provider) = Create();
        await using (provider) {
            var id = await storage.SaveSnapshotAsync([1, 2, 3], runId, stepId, CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(id));
            Assert.Equal(new byte[] { 1, 2, 3 }, await storage.ReadSnapshotAsync(id!, CancellationToken.None));
            Assert.Equal(AutomationRunStoragePaths.Step(runId, stepId), memory.LastPrefix);
            Assert.Equal(AutomationRunStoragePaths.ScreenshotFileName(stepId), memory.LastFileName);
            Assert.Equal("image/png", memory.LastContentType);
        }
    }

    [Fact]
    public async Task Save_StoresExtractionUnderTheRunOrTheStep()
    {
        var runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var stepId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var (storage, memory, provider) = Create();
        await using (provider) {
            await storage.SaveAsync([1], "summary.json", AutomationRunStoragePaths.Run(runId), "application/json", CancellationToken.None);
            Assert.Equal(AutomationRunStoragePaths.Run(runId), memory.LastPrefix);
            Assert.Equal("summary.json", memory.LastFileName);

            await storage.SaveAsync([2], "rows.json", AutomationRunStoragePaths.Step(runId, stepId), "application/json", CancellationToken.None);
            Assert.Equal(AutomationRunStoragePaths.Step(runId, stepId), memory.LastPrefix);
            Assert.Equal("rows.json", memory.LastFileName);
            Assert.Equal("application/json", memory.LastContentType);
        }
    }

    private static (IAutomationPlanFileStorage Storage, MemoryPlanFiles Files, ServiceProvider Provider) Create()
    {
        var files = new Dictionary<Guid, byte[]>();
        var memory = new MemoryPlanFiles(files);
        var services = new ServiceCollection();
        services.AddScoped<IFileStorageService>(_ => memory);
        services.AddAutomationPlanFileStorage(sp => sp.GetRequiredService<IFileStorageService>());
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        return (provider.GetRequiredService<IAutomationPlanFileStorage>(), memory, provider);
    }

    private sealed class MemoryPlanFiles : IFileStorageService
    {
        private readonly Dictionary<Guid, byte[]> _files;

        public MemoryPlanFiles(Dictionary<Guid, byte[]> files) => _files = files;

        public string? LastPrefix { get; private set; }

        public string? LastFileName { get; private set; }

        public string? LastContentType { get; private set; }

        public string HealthCheckName => nameof(MemoryPlanFiles);

        public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero));

        public Task<FileStoreResult> SaveFileAsync(byte[] data, string? originalFileName = null, bool compress = false, bool encrypt = false, string? keyId = null, string? pathPrefix = null, int? chunkSize = null, string? contentType = null, string? charset = null, string? tenantId = null, JsonElement? metadata = null, CancellationToken ct = default)
        {
            LastPrefix = pathPrefix;
            LastFileName = originalFileName;
            LastContentType = contentType;
            var id = Guid.NewGuid();
            _files[id] = data;
            return Task.FromResult(new FileStoreResult(id, originalFileName, data.LongLength, [], originalFileName ?? "x.bin", data.LongLength, [], false, null, null, null, false, null, null, null, null, null, null, null, null, DateTime.UtcNow, pathPrefix, ContentType: contentType));
        }

        public Task<byte[]> GetFileAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default)
            => Task.FromResult(_files[fileId]);

        public Task<FileStoreResult> SaveFileAsync(string filePath, string? originalFileName = null, bool compress = false, bool encrypt = false, string? keyId = null, string? pathPrefix = null, int? chunkSize = null, string? contentType = null, string? charset = null, string? tenantId = null, JsonElement? metadata = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<FileStoreResult> SaveFromStreamAsync(Stream input, long declaredLength, string? originalFileName = null, bool compress = false, bool encrypt = false, string? keyId = null, string? pathPrefix = null, int? chunkSize = null, string? contentType = null, string? charset = null, string? tenantId = null, FileAvailability? availabilityOverride = null, Guid? fileId = null, JsonElement? metadata = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration = null, string? pathPrefix = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<string> GetPreSignedReadUrlAsync(Guid fileId, TimeSpan? expiration, string? pathPrefix, PreSignedReadUrlOptions? urlResponseOptions, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<DirectUploadBeginResult> BeginDirectUploadAsync(DirectUploadBeginRequest request, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<FileStoreResult> CompleteDirectUploadAsync(Guid fileId, DirectUploadCompleteRequest? completeRequest = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<int> SweepExpiredDirectUploadsAsync(int maxUploads = 1000, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<FileStoreResult> CopyFileAsync(Guid sourceFileId, CopyFileRequest? request = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<FileStoreResult> MoveFileAsync(Guid fileId, MoveFileRequest request, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<FileStoreResult> RenameFileAsync(Guid fileId, RenameFileRequest request, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<Stream?> GetFileStreamAsync(Guid fileId, CompressionAlgorithm? compressionAlgorithmOverride = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> DeleteFileAsync(Guid fileId, FileDeletionMode mode = FileDeletionMode.RemoveObjectAndTombstoneMetadata, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default) => throw new NotSupportedException();

        public async Task<FileContentIntegrity> GetContentIntegrityAsync(Guid fileId, CancellationToken ct = default)
            => FileContentIntegrity.From(await GetMetadataAsync(fileId, ct).ConfigureAwait(false));

        public Task<DekMigrationResult> MigrateDeksAsync(string sourceKeyId, string? sourceKeyVersion = null, string? targetKeyId = null, string? targetKeyVersion = null, int batchSize = 100, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<DekMigrationResult> RotateDeksAsync(IReadOnlyCollection<Guid> fileIds, string? targetKeyId = null, string? targetKeyVersion = null, int batchSize = 100, CancellationToken ct = default) => throw new NotSupportedException();

#pragma warning disable CS0067
        public event EventHandler<FileSavedResult>? FileSaved;
        public event EventHandler<FileRetrievedResult>? FileRetrieved;
        public event EventHandler<FileDeletedResult>? FileDeleted;
        public event EventHandler<FileMovedResult>? FileMoved;
        public event EventHandler<FileRenamedResult>? FileRenamed;
        public event EventHandler<FileMetadataRetrievedResult>? FileMetadataRetrieved;
        public event EventHandler<FileAuditEventArgs>? FileAuditOccurred;
#pragma warning restore CS0067
    }
}
