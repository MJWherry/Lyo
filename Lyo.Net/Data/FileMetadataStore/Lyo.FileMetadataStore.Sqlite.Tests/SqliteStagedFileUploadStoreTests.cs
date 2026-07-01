using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Sqlite.Tests;

public sealed class SqliteStagedFileUploadStoreTests
{
    private readonly FileMetadataSqliteFixture _fixture;

    public SqliteStagedFileUploadStoreTests(FileMetadataSqliteFixture fixture) => _fixture = fixture;

    private SqliteStagedFileUploadStore CreateStore()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<SqliteFileMetadataStoreDbContext>>();
        return new(factory);
    }

    private static StagedFileUploadRecord SampleRecord(Guid? stageId = null)
    {
        var id = stageId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        return new(
            id, "tenant-a", Guid.NewGuid(), now, now.AddHours(24), StagedUploadStatus.PendingUpload, $"files/.stage/{id:N}/object", "uploads", "file.bin",
            "application/octet-stream", 1024, null, null, HashAlgorithm.Sha256, MultipartUploadProviderKind.AzureBlob, "{}", null, null);
    }

    [Fact]
    public async Task CreateAsync_AndGetAsync_PersistsAndRetrieves()
    {
        var store = CreateStore();
        var record = SampleRecord();
        await store.CreateAsync(record, TestContext.Current.CancellationToken);
        var retrieved = await store.GetAsync(record.StageId, TestContext.Current.CancellationToken);
        Assert.NotNull(retrieved);
        Assert.Equal(record.StageId, retrieved.StageId);
        Assert.Equal(record.TenantId, retrieved.TenantId);
        Assert.Equal(record.OwnerId, retrieved.OwnerId);
        Assert.Equal(record.Status, retrieved.Status);
        Assert.Equal(record.StorageLocation, retrieved.StorageLocation);
        Assert.Equal(record.PathPrefix, retrieved.PathPrefix);
        Assert.Equal(record.OriginalFileName, retrieved.OriginalFileName);
        Assert.Equal(record.ContentType, retrieved.ContentType);
        Assert.Equal(record.DeclaredMaxSizeBytes, retrieved.DeclaredMaxSizeBytes);
        Assert.Equal(record.ProviderKind, retrieved.ProviderKind);
        Assert.Equal(record.ProviderStateJson, retrieved.ProviderStateJson);
        Assert.Equal(record.HashAlgorithm, retrieved.HashAlgorithm);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ReturnsNull()
    {
        var store = CreateStore();
        var retrieved = await store.GetAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var store = CreateStore();
        var record = SampleRecord();
        await store.CreateAsync(record, TestContext.Current.CancellationToken);
        var hash = new byte[] { 9, 8, 7, 6 };
        var updated = record with { Status = StagedUploadStatus.Uploaded, ObservedSizeBytes = 512, ContentHash = hash };
        await store.UpdateAsync(updated, TestContext.Current.CancellationToken);
        var retrieved = await store.GetAsync(record.StageId, TestContext.Current.CancellationToken);
        Assert.NotNull(retrieved);
        Assert.Equal(StagedUploadStatus.Uploaded, retrieved.Status);
        Assert.Equal(512, retrieved.ObservedSizeBytes);
        Assert.NotNull(retrieved.ContentHash);
        Assert.True(retrieved.ContentHash.SequenceEqual(hash));
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_Throws()
    {
        var store = CreateStore();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => store.UpdateAsync(SampleRecord(), TestContext.Current.CancellationToken));
        Assert.Contains("was not found", ex.Message);
    }

    [Fact]
    public async Task TryTransitionStatusAsync_WhenFromMatches_UpdatesStatus()
    {
        var store = CreateStore();
        var record = SampleRecord();
        await store.CreateAsync(record, TestContext.Current.CancellationToken);
        Assert.True(await store.TryTransitionStatusAsync(record.StageId, StagedUploadStatus.PendingUpload, StagedUploadStatus.Uploaded, TestContext.Current.CancellationToken));
        var retrieved = await store.GetAsync(record.StageId, TestContext.Current.CancellationToken);
        Assert.NotNull(retrieved);
        Assert.Equal(StagedUploadStatus.Uploaded, retrieved.Status);
    }

    [Fact]
    public async Task TryTransitionStatusAsync_WhenFromMismatch_ReturnsFalse()
    {
        var store = CreateStore();
        var record = SampleRecord();
        await store.CreateAsync(record, TestContext.Current.CancellationToken);
        Assert.False(await store.TryTransitionStatusAsync(record.StageId, StagedUploadStatus.Uploaded, StagedUploadStatus.Committed, TestContext.Current.CancellationToken));
        var retrieved = await store.GetAsync(record.StageId, TestContext.Current.CancellationToken);
        Assert.NotNull(retrieved);
        Assert.Equal(StagedUploadStatus.PendingUpload, retrieved.Status);
    }
}