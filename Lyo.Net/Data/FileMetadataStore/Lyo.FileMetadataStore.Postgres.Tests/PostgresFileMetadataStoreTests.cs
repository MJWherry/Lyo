using Lyo.Encryption;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Postgres.Tests;

public class PostgresFileMetadataStoreTests
{
    private readonly FileMetadataPostgresFixture _fixture;

    public PostgresFileMetadataStoreTests(FileMetadataPostgresFixture fixture) => _fixture = fixture;

    private static FileStoreResult CreateMetadata(Guid id, byte[] hash, string? keyId = null, string? keyVersion = null)
        => new(
            id, "original.pdf", 1024, hash, "source.pdf", 1024, hash, false, null, null, null, keyId != null, keyId != null ? EncryptionAlgorithm.AesGcm : null,
            keyId != null ? EncryptionAlgorithm.AesGcm : null, null, null, null, keyId, keyVersion, null, DateTime.UtcNow, null, HashAlgorithm.Sha256);

    [Fact]
    public async Task SaveMetadataAsync_AndGetMetadataAsync_PersistsAndRetrieves()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var hash = new byte[] { 1, 2, 3, 4, 5 };
        var metadata = CreateMetadata(fileId, hash);
        await store.SaveMetadataAsync(fileId, metadata, TestContext.Current.CancellationToken);
        var retrieved = await store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken);
        Assert.Equal(fileId, retrieved.Id);
        Assert.Equal("original.pdf", retrieved.OriginalFileName);
        Assert.Equal(1024, retrieved.OriginalFileSize);
        Assert.True(retrieved.OriginalFileHash.SequenceEqual(hash));
        Assert.Equal(HashAlgorithm.Sha256, retrieved.HashAlgorithm);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenNotFound_ThrowsFileNotFoundException()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken));
        Assert.Contains(fileId.ToString(), ex.Message);
    }

    [Fact]
    public async Task SaveMetadataAsync_WithNullMetadata_ThrowsArgumentNullException()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveMetadataAsync(fileId, null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteMetadataAsync_WhenExists_ReturnsTrue()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        var store = new PostgresFileMetadataStore(context);
        var fileId = Guid.NewGuid();
        var metadata = CreateMetadata(fileId, [1, 2, 3]);
        await store.SaveMetadataAsync(fileId, metadata, TestContext.Current.CancellationToken);
        var result = await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken);
        Assert.True(result);
        await Assert.ThrowsAsync<FileNotFoundException>(() => store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken));

        var entity = await context.FileMetadata.AsNoTracking().SingleAsync(e => e.Id == fileId.ToString(), TestContext.Current.CancellationToken);
        Assert.NotNull(entity.DeletedAt);
    }

    [Fact]
    public async Task DeleteMetadataAsync_WhenNotExists_ReturnsFalse()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var result = await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken);
        Assert.False(result);
    }

    [Fact]
    public async Task PurgeMetadataAsync_WhenExists_RemovesRow_Idempotent()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        var store = new PostgresFileMetadataStore(context);
        var fileId = Guid.NewGuid();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [1, 2]), TestContext.Current.CancellationToken);
        Assert.True(await store.PurgeMetadataAsync(fileId, TestContext.Current.CancellationToken));
        Assert.False(await context.FileMetadata.AsNoTracking().AnyAsync(e => e.Id == fileId.ToString(), TestContext.Current.CancellationToken));
        Assert.False(await store.PurgeMetadataAsync(fileId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PurgeMetadataAsync_AfterSoftDelete_RemovesRow()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        var store = new PostgresFileMetadataStore(context);
        var fileId = Guid.NewGuid();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [7]), TestContext.Current.CancellationToken);
        Assert.True(await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken));
        Assert.True(await store.PurgeMetadataAsync(fileId, TestContext.Current.CancellationToken));
        Assert.False(await context.FileMetadata.AsNoTracking().AnyAsync(e => e.Id == fileId.ToString(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveMetadataAsync_UpdateExisting_Overwrites()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var metadata1 = CreateMetadata(fileId, [1, 2, 3]);
        var metadata2 = CreateMetadata(fileId, [6, 7, 8]) with { OriginalFileName = "updated.pdf" };
        await store.SaveMetadataAsync(fileId, metadata1, TestContext.Current.CancellationToken);
        await store.SaveMetadataAsync(fileId, metadata2, TestContext.Current.CancellationToken);
        var retrieved = await store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken);
        Assert.Equal("updated.pdf", retrieved.OriginalFileName);
        Assert.True(retrieved.OriginalFileHash.SequenceEqual(new byte[] { 6, 7, 8 }));
    }

    [Fact]
    public async Task FindByHashAsync_WhenFound_ReturnsMetadata()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var hash = new byte[] { 10, 20, 30, 40, 50 };
        var metadata = CreateMetadata(fileId, hash);
        await store.SaveMetadataAsync(fileId, metadata, TestContext.Current.CancellationToken);
        var result = await store.FindByHashAsync(hash, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(fileId, result.Id);
        Assert.True(result.OriginalFileHash.SequenceEqual(hash));
    }

    [Fact]
    public async Task FindByHashAsync_WhenNotFound_ReturnsNull()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var hash = "cba`_"u8.ToArray();
        var result = await store.FindByHashAsync(hash, TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByHashAsync_WithNullHash_ThrowsArgumentNullException()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByHashAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByHashAsync_WithEmptyHash_ThrowsArgumentException()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        await Assert.ThrowsAsync<ArgumentException>(() => store.FindByHashAsync([], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WhenFound_ReturnsMatchingFiles()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var keyId = "key-123";
        var version = "v1";
        var file1 = CreateMetadata(Guid.NewGuid(), [1], keyId, version);
        var file2 = CreateMetadata(Guid.NewGuid(), [2], keyId, version);
        await store.SaveMetadataAsync(file1.Id, file1, TestContext.Current.CancellationToken);
        await store.SaveMetadataAsync(file2.Id, file2, TestContext.Current.CancellationToken);
        var results = (await store.FindByKeyIdAndVersionAsync(keyId, version, TestContext.Current.CancellationToken)).ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Id == file1.Id);
        Assert.Contains(results, r => r.Id == file2.Id);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithNullKeyVersion_ReturnsAllVersionsForKey()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var keyId = "key-multi";
        var file1 = CreateMetadata(Guid.NewGuid(), [1], keyId, "v1");
        var file2 = CreateMetadata(Guid.NewGuid(), [2], keyId, "v2");
        await store.SaveMetadataAsync(file1.Id, file1, TestContext.Current.CancellationToken);
        await store.SaveMetadataAsync(file2.Id, file2, TestContext.Current.CancellationToken);
        var results = (await store.FindByKeyIdAndVersionAsync(keyId, null, TestContext.Current.CancellationToken)).ToList();
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithNullKeyId_ThrowsArgumentNullException()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByKeyIdAndVersionAsync(null!, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithEmptyKeyId_ThrowsArgumentException()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        await Assert.ThrowsAsync<ArgumentException>(() => store.FindByKeyIdAndVersionAsync("", null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WhenNoMatches_ReturnsEmpty()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var results = (await store.FindByKeyIdAndVersionAsync("nonexistent-key", null, TestContext.Current.CancellationToken)).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        var store = new PostgresFileMetadataStore(context);
        store.Dispose();
        store.Dispose();
    }

    [Fact]
    public async Task FindByHashAsync_WithDifferentLengthHash_ReturnsNull()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var hash = new byte[] { 1, 2, 3, 4, 5 };
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, hash), TestContext.Current.CancellationToken);
        var shortHash = new byte[] { 1, 2, 3 };
        var result = await store.FindByHashAsync(shortHash, TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteMetadataAsync_WhenAlreadyDeleted_ReturnsFalse()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [1]), TestContext.Current.CancellationToken);
        Assert.True(await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken));
        Assert.False(await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByHashAsync_AfterSoftDelete_IgnoresTombstone()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var hash = new byte[] { 9, 9, 9, 1 };
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, hash), TestContext.Current.CancellationToken);
        await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken);
        Assert.Null(await store.FindByHashAsync(hash, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveMetadataAsync_AfterSoftDeleteWithSameId_ClearsDeletedAt()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var refreshed = CreateMetadata(fileId, [4, 5, 6]);
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [1, 2]), TestContext.Current.CancellationToken);
        await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken);
        await store.SaveMetadataAsync(fileId, refreshed, TestContext.Current.CancellationToken);
        var got = await store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken);
        Assert.Null(got.DeletedAt);
        Assert.True(got.OriginalFileHash.SequenceEqual(new byte[] { 4, 5, 6 }));
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_ExcludesSoftDeleted()
    {
        Assert.NotNull(_fixture.ServiceProvider);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        const string keyId = "k1";
        const string version = "v1";
        var active = CreateMetadata(Guid.NewGuid(), [1], keyId, version);
        var removed = CreateMetadata(Guid.NewGuid(), [2], keyId, version);
        await store.SaveMetadataAsync(active.Id, active, TestContext.Current.CancellationToken);
        await store.SaveMetadataAsync(removed.Id, removed, TestContext.Current.CancellationToken);
        await store.DeleteMetadataAsync(removed.Id, TestContext.Current.CancellationToken);
        var list = (await store.FindByKeyIdAndVersionAsync(keyId, version, TestContext.Current.CancellationToken)).ToList();
        Assert.Single(list);
        Assert.Equal(active.Id, list[0].Id);
    }
}