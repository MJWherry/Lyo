using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Encryption;
using Lyo.FileMetadataStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Tests;

/// <summary>Shared IFileMetadataStore contract tests for Postgres and SQLite. Provider suites inherit and supply store + DbSet.</summary>
public abstract class FileMetadataStoreContractTests
{
    /// <summary>Root fixture provider.</summary>
    protected abstract IServiceProvider Services { get; }

    /// <summary>Store bound to the scoped context.</summary>
    protected abstract IFileMetadataStore CreateStore(IServiceScope scope);

    /// <summary>Metadata set on the same scoped context as <see cref="CreateStore" />.</summary>
    protected abstract DbSet<FileMetadataEntity> Metadata(IServiceScope scope);

    private static FileStoreResult CreateMetadata(Guid id, byte[] hash, string? keyId = null, string? keyVersion = null)
        => new(
            id, "original.pdf", 1024, hash, "source.pdf", 1024, hash, false, null, null, null, keyId != null, keyId != null ? EncryptionAlgorithm.AesGcm : null,
            keyId != null ? EncryptionAlgorithm.AesGcm : null, null, null, null, keyId, keyVersion, null, DateTime.UtcNow, null, HashAlgorithm.Sha256);

    [Fact]
    public async Task SaveMetadataAsync_AndGetMetadataAsync_PersistsAndRetrieves()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var hash = new byte[] { 1, 2, 3, 4, 5 };
        var metadata = CreateMetadata(fileId, hash);
        await store.SaveMetadataAsync(fileId, metadata, TestCt);
        var retrieved = await store.GetMetadataAsync(fileId, TestCt);
        Assert.Equal(fileId, retrieved.Id);
        Assert.Equal("original.pdf", retrieved.OriginalFileName);
        Assert.Equal(1024, retrieved.OriginalFileSize);
        Assert.True(retrieved.OriginalFileHash.SequenceEqual(hash));
        Assert.Equal(HashAlgorithm.Sha256, retrieved.HashAlgorithm);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenNotFound_ThrowsFileNotFoundException()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => store.GetMetadataAsync(fileId, TestCt));
        Assert.Contains(fileId.ToString(), ex.Message);
    }

    [Fact]
    public async Task SaveMetadataAsync_WithNullMetadata_ThrowsArgumentNullException()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveMetadataAsync(fileId, null!, TestCt));
    }

    [Fact]
    public async Task DeleteMetadataAsync_WhenExists_ReturnsTrue()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var metadataSet = Metadata(scope);
        var fileId = LyoGuid.CreateV7();
        var metadata = CreateMetadata(fileId, [1, 2, 3]);
        await store.SaveMetadataAsync(fileId, metadata, TestCt);
        var result = await store.DeleteMetadataAsync(fileId, TestCt);
        Assert.True(result);
        await Assert.ThrowsAsync<FileNotFoundException>(() => store.GetMetadataAsync(fileId, TestCt));
        var entity = await metadataSet.AsNoTracking().SingleAsync(e => e.Id == fileId.ToString(), TestCt);
        Assert.NotNull(entity.DeletedAt);
        Assert.Equal(nameof(FileAvailability.Deleted), entity.Availability);
    }

    [Fact]
    public async Task DeleteMetadataAsync_WhenNotExists_ReturnsFalse()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var result = await store.DeleteMetadataAsync(fileId, TestCt);
        Assert.False(result);
    }

    [Fact]
    public async Task PurgeMetadataAsync_WhenExists_RemovesRow_Idempotent()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var metadataSet = Metadata(scope);
        var fileId = LyoGuid.CreateV7();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [1, 2]), TestCt);
        Assert.True(await store.PurgeMetadataAsync(fileId, TestCt));
        Assert.False(await metadataSet.AsNoTracking().AnyAsync(e => e.Id == fileId.ToString(), TestCt));
        Assert.False(await store.PurgeMetadataAsync(fileId, TestCt));
    }

    [Fact]
    public async Task PurgeMetadataAsync_AfterSoftDelete_RemovesRow()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var metadataSet = Metadata(scope);
        var fileId = LyoGuid.CreateV7();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [7]), TestCt);
        Assert.True(await store.DeleteMetadataAsync(fileId, TestCt));
        Assert.True(await store.PurgeMetadataAsync(fileId, TestCt));
        Assert.False(await metadataSet.AsNoTracking().AnyAsync(e => e.Id == fileId.ToString(), TestCt));
    }

    [Fact]
    public async Task SaveMetadataAsync_UpdateExisting_Overwrites()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var metadata1 = CreateMetadata(fileId, [1, 2, 3]);
        var metadata2 = CreateMetadata(fileId, [6, 7, 8]) with { OriginalFileName = "updated.pdf" };
        await store.SaveMetadataAsync(fileId, metadata1, TestCt);
        await store.SaveMetadataAsync(fileId, metadata2, TestCt);
        var retrieved = await store.GetMetadataAsync(fileId, TestCt);
        Assert.Equal("updated.pdf", retrieved.OriginalFileName);
        Assert.True(retrieved.OriginalFileHash.SequenceEqual(new byte[] { 6, 7, 8 }));
    }

    [Fact]
    public async Task FindByHashAsync_WhenFound_ReturnsMetadata()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var hash = new byte[] { 10, 20, 30, 40, 50 };
        var metadata = CreateMetadata(fileId, hash);
        await store.SaveMetadataAsync(fileId, metadata, TestCt);
        var result = await store.FindByHashAsync(hash, TestCt);
        Assert.NotNull(result);
        Assert.Equal(fileId, result.Id);
        Assert.True(result.OriginalFileHash.SequenceEqual(hash));
    }

    [Fact]
    public async Task FindByHashAsync_WhenNotFound_ReturnsNull()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var hash = "cba`_"u8.ToArray();
        var result = await store.FindByHashAsync(hash, TestCt);
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByHashAsync_WithNullHash_ThrowsArgumentNullException()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByHashAsync(null!, TestCt));
    }

    [Fact]
    public async Task FindByHashAsync_WithEmptyHash_ThrowsArgumentException()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        await Assert.ThrowsAsync<ArgumentException>(() => store.FindByHashAsync([], TestCt));
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WhenFound_ReturnsMatchingFiles()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var keyId = "key-123";
        var version = "v1";
        var file1 = CreateMetadata(LyoGuid.CreateV7(), [1], keyId, version);
        var file2 = CreateMetadata(LyoGuid.CreateV7(), [2], keyId, version);
        await store.SaveMetadataAsync(file1.Id, file1, TestCt);
        await store.SaveMetadataAsync(file2.Id, file2, TestCt);
        var results = (await store.FindByKeyIdAndVersionAsync(keyId, version, TestCt)).ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Id == file1.Id);
        Assert.Contains(results, r => r.Id == file2.Id);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithNullKeyVersion_ReturnsAllVersionsForKey()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var keyId = "key-multi";
        var file1 = CreateMetadata(LyoGuid.CreateV7(), [1], keyId, "v1");
        var file2 = CreateMetadata(LyoGuid.CreateV7(), [2], keyId, "v2");
        await store.SaveMetadataAsync(file1.Id, file1, TestCt);
        await store.SaveMetadataAsync(file2.Id, file2, TestCt);
        var results = (await store.FindByKeyIdAndVersionAsync(keyId, null, TestCt)).ToList();
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithNullKeyId_ThrowsArgumentNullException()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByKeyIdAndVersionAsync(null!, null, TestCt));
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithEmptyKeyId_ThrowsArgumentException()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        await Assert.ThrowsAsync<ArgumentException>(() => store.FindByKeyIdAndVersionAsync("", null, TestCt));
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WhenNoMatches_ReturnsEmpty()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var results = (await store.FindByKeyIdAndVersionAsync("nonexistent-key", null, TestCt)).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        ((IDisposable)store).Dispose();
        ((IDisposable)store).Dispose();
    }

    [Fact]
    public async Task FindByHashAsync_WithDifferentLengthHash_ReturnsNull()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var hash = new byte[] { 201, 202, 203, 204, 205 };
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, hash), TestCt);
        var shortHash = new byte[] { 201, 202, 203 };
        var result = await store.FindByHashAsync(shortHash, TestCt);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteMetadataAsync_WhenAlreadyDeleted_ReturnsFalse()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [1]), TestCt);
        Assert.True(await store.DeleteMetadataAsync(fileId, TestCt));
        Assert.False(await store.DeleteMetadataAsync(fileId, TestCt));
    }

    [Fact]
    public async Task FindByHashAsync_AfterSoftDelete_IgnoresTombstone()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var hash = new byte[] { 9, 9, 9, 1 };
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, hash), TestCt);
        await store.DeleteMetadataAsync(fileId, TestCt);
        Assert.Null(await store.FindByHashAsync(hash, TestCt));
    }

    [Fact]
    public async Task SaveMetadataAsync_AfterSoftDeleteWithSameId_ClearsDeletedAt()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var refreshed = CreateMetadata(fileId, [4, 5, 6]);
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [1, 2]), TestCt);
        await store.DeleteMetadataAsync(fileId, TestCt);
        await store.SaveMetadataAsync(fileId, refreshed, TestCt);
        var got = await store.GetMetadataAsync(fileId, TestCt);
        Assert.Null(got.DeletedAt);
        Assert.True(got.OriginalFileHash.SequenceEqual(new byte[] { 4, 5, 6 }));
    }

    [Fact]
    public async Task ListByPathPrefixAsync_ImmediateAndDescendants()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var root = CreateMetadata(LyoGuid.CreateV7(), [1]) with { OriginalFileName = "root.txt" };
        var reports = CreateMetadata(LyoGuid.CreateV7(), [2]) with { PathPrefix = "reports", OriginalFileName = "a.pdf" };
        var nested = CreateMetadata(LyoGuid.CreateV7(), [3]) with { PathPrefix = "reports/2024", OriginalFileName = "b.pdf" };
        await store.SaveMetadataAsync(root.Id, root, TestCt);
        await store.SaveMetadataAsync(reports.Id, reports, TestCt);
        await store.SaveMetadataAsync(nested.Id, nested, TestCt);
        var immediate = await store.ListByPathPrefixAsync("reports", false, 50, TestCt);
        Assert.Single(immediate);
        Assert.Equal(reports.Id, immediate[0].Id);
        var descendants = await store.ListByPathPrefixAsync("reports", true, 50, TestCt);
        Assert.Equal(2, descendants.Count);
        var atRoot = await store.ListByPathPrefixAsync(null, false, 50, TestCt);
        Assert.Contains(atRoot, r => r.Id == root.Id);
        Assert.DoesNotContain(atRoot, r => r.Id == reports.Id);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_ExcludesSoftDeleted()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        const string keyId = "k1";
        const string version = "v1";
        var active = CreateMetadata(LyoGuid.CreateV7(), [1], keyId, version);
        var removed = CreateMetadata(LyoGuid.CreateV7(), [2], keyId, version);
        await store.SaveMetadataAsync(active.Id, active, TestCt);
        await store.SaveMetadataAsync(removed.Id, removed, TestCt);
        await store.DeleteMetadataAsync(removed.Id, TestCt);
        var list = (await store.FindByKeyIdAndVersionAsync(keyId, version, TestCt)).ToList();
        Assert.Single(list);
        Assert.Equal(active.Id, list[0].Id);
    }

    [Fact]
    public async Task SaveMetadataAsync_PersistsOpaqueMetadataJson()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        using var doc = JsonDocument.Parse("""{"source":"scan","n":1}""");
        var metadata = CreateMetadata(fileId, [9, 8, 7]) with { Metadata = doc.RootElement.Clone() };
        await store.SaveMetadataAsync(fileId, metadata, TestCt);
        var retrieved = await store.GetMetadataAsync(fileId, TestCt);
        Assert.NotNull(retrieved.Metadata);
        Assert.Equal(JsonValueKind.Object, retrieved.Metadata.Value.ValueKind);
        Assert.Equal("scan", retrieved.Metadata.Value.GetProperty("source").GetString());
        Assert.Equal(1, retrieved.Metadata.Value.GetProperty("n").GetInt32());
    }

    [Fact]
    public async Task ListPendingDirectUploadsAsync_ReturnsOnlyLivePendingRowsBeganBeforeCutoff()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var now = DateTime.UtcNow;
        var stale = CreateMetadata(LyoGuid.CreateV7(), []) with { Availability = FileAvailability.PendingDirectUpload, Timestamp = now.AddHours(-2) };
        var fresh = CreateMetadata(LyoGuid.CreateV7(), []) with { Availability = FileAvailability.PendingDirectUpload, Timestamp = now };
        var finalized = CreateMetadata(LyoGuid.CreateV7(), [1]) with { Availability = FileAvailability.Available, Timestamp = now.AddHours(-2) };
        var tombstoned = CreateMetadata(LyoGuid.CreateV7(), []) with { Availability = FileAvailability.PendingDirectUpload, Timestamp = now.AddHours(-2) };
        foreach (var row in new[] { stale, fresh, finalized, tombstoned })
            await store.SaveMetadataAsync(row.Id, row, TestCt);

        await store.DeleteMetadataAsync(tombstoned.Id, TestCt);
        var expired = await store.ListPendingDirectUploadsAsync(now.AddHours(-1), 1000, TestCt);
        Assert.Contains(expired, r => r.Id == stale.Id);
        Assert.DoesNotContain(expired, r => r.Id == fresh.Id || r.Id == finalized.Id || r.Id == tombstoned.Id);
    }

    [Fact]
    public async Task ListPendingDirectUploadsAsync_CapsAtMaxKeys()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var old = DateTime.UtcNow.AddDays(-1);
        for (var i = 0; i < 3; i++) {
            var row = CreateMetadata(LyoGuid.CreateV7(), []) with { Availability = FileAvailability.PendingDirectUpload, Timestamp = old };
            await store.SaveMetadataAsync(row.Id, row, TestCt);
        }

        Assert.Equal(2, (await store.ListPendingDirectUploadsAsync(DateTime.UtcNow, 2, TestCt)).Count);
    }

    [Fact]
    public async Task SaveMetadataAsync_NullMetadata_StaysNull()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = CreateStore(scope);
        var fileId = LyoGuid.CreateV7();
        var metadata = CreateMetadata(fileId, [1, 2, 3]);
        await store.SaveMetadataAsync(fileId, metadata, TestCt);
        var retrieved = await store.GetMetadataAsync(fileId, TestCt);
        Assert.Null(retrieved.Metadata);
    }
}