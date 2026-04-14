using Lyo.Encryption;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Lyo.FileMetadataStore.Postgres.Tests;

public class PostgresFileMetadataStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly ITestOutputHelper _output;
    private IServiceProvider? _serviceProvider;

    public PostgresFileMetadataStoreTests(ITestOutputHelper output) => _output = output;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        var connectionString = _container.GetConnectionString();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddDbContext<FileMetadataStoreDbContext>(opts => opts.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "filestore")));
        _serviceProvider = services.BuildServiceProvider();
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    private static FileStoreResult CreateMetadata(Guid id, byte[] hash, string? keyId = null, string? keyVersion = null)
        => new(
            id, "original.pdf", 1024, hash, "source.pdf", 1024, hash, false, null, null, null, keyId != null, keyId != null ? EncryptionAlgorithm.AesGcm : null,
            keyId != null ? EncryptionAlgorithm.AesGcm : null, null, null, null, keyId, keyVersion, null, DateTime.UtcNow, null, HashAlgorithm.Sha256);

    [Fact]
    public async Task SaveMetadataAsync_AndGetMetadataAsync_PersistsAndRetrieves()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var hash = new byte[] { 1, 2, 3, 4, 5 };
        var metadata = CreateMetadata(fileId, hash);
        await store.SaveMetadataAsync(fileId, metadata, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrieved = await store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(fileId, retrieved.Id);
        Assert.Equal("original.pdf", retrieved.OriginalFileName);
        Assert.Equal(1024, retrieved.OriginalFileSize);
        Assert.True(retrieved.OriginalFileHash.SequenceEqual(hash));
        Assert.Equal(HashAlgorithm.Sha256, retrieved.HashAlgorithm);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenNotFound_ThrowsFileNotFoundException()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken)).ConfigureAwait(false);
        Assert.Contains(fileId.ToString(), ex.Message);
    }

    [Fact]
    public async Task SaveMetadataAsync_WithNullMetadata_ThrowsArgumentNullException()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveMetadataAsync(fileId, null!, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task DeleteMetadataAsync_WhenExists_ReturnsTrue()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var metadata = CreateMetadata(fileId, [1, 2, 3]);
        await store.SaveMetadataAsync(fileId, metadata, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result);
        await Assert.ThrowsAsync<FileNotFoundException>(() => store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task DeleteMetadataAsync_WhenNotExists_ReturnsFalse()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var result = await store.DeleteMetadataAsync(fileId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result);
    }

    [Fact]
    public async Task SaveMetadataAsync_UpdateExisting_Overwrites()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var metadata1 = CreateMetadata(fileId, [1, 2, 3]);
        var metadata2 = CreateMetadata(fileId, [6, 7, 8]) with { OriginalFileName = "updated.pdf" };
        await store.SaveMetadataAsync(fileId, metadata1, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await store.SaveMetadataAsync(fileId, metadata2, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrieved = await store.GetMetadataAsync(fileId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("updated.pdf", retrieved.OriginalFileName);
        Assert.True(retrieved.OriginalFileHash.SequenceEqual(new byte[] { 6, 7, 8 }));
    }

    [Fact]
    public async Task FindByHashAsync_WhenFound_ReturnsMetadata()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var hash = new byte[] { 10, 20, 30, 40, 50 };
        var metadata = CreateMetadata(fileId, hash);
        await store.SaveMetadataAsync(fileId, metadata, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await store.FindByHashAsync(hash, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(fileId, result.Id);
        Assert.True(result.OriginalFileHash.SequenceEqual(hash));
    }

    [Fact]
    public async Task FindByHashAsync_WhenNotFound_ReturnsNull()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var hash = "cba`_"u8.ToArray();
        var result = await store.FindByHashAsync(hash, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByHashAsync_WithNullHash_ThrowsArgumentNullException()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByHashAsync(null!, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task FindByHashAsync_WithEmptyHash_ThrowsArgumentException()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        await Assert.ThrowsAsync<ArgumentException>(() => store.FindByHashAsync([], TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WhenFound_ReturnsMatchingFiles()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var keyId = "key-123";
        var version = "v1";
        var file1 = CreateMetadata(Guid.NewGuid(), [1], keyId, version);
        var file2 = CreateMetadata(Guid.NewGuid(), [2], keyId, version);
        await store.SaveMetadataAsync(file1.Id, file1, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await store.SaveMetadataAsync(file2.Id, file2, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var results = (await store.FindByKeyIdAndVersionAsync(keyId, version, TestContext.Current.CancellationToken).ConfigureAwait(false)).ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Id == file1.Id);
        Assert.Contains(results, r => r.Id == file2.Id);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithNullKeyVersion_ReturnsAllVersionsForKey()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var keyId = "key-multi";
        var file1 = CreateMetadata(Guid.NewGuid(), [1], keyId, "v1");
        var file2 = CreateMetadata(Guid.NewGuid(), [2], keyId, "v2");
        await store.SaveMetadataAsync(file1.Id, file1, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await store.SaveMetadataAsync(file2.Id, file2, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var results = (await store.FindByKeyIdAndVersionAsync(keyId, null, TestContext.Current.CancellationToken).ConfigureAwait(false)).ToList();
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithNullKeyId_ThrowsArgumentNullException()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByKeyIdAndVersionAsync(null!, null, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WithEmptyKeyId_ThrowsArgumentException()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        await Assert.ThrowsAsync<ArgumentException>(() => store.FindByKeyIdAndVersionAsync("", null, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task FindByKeyIdAndVersionAsync_WhenNoMatches_ReturnsEmpty()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var results = (await store.FindByKeyIdAndVersionAsync("nonexistent-key", null, TestContext.Current.CancellationToken).ConfigureAwait(false)).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        var store = new PostgresFileMetadataStore(context);
        store.Dispose();
        store.Dispose();
    }

    [Fact]
    public async Task FindByHashAsync_WithDifferentLengthHash_ReturnsNull()
    {
        Assert.NotNull(_serviceProvider);
        using var scope = _serviceProvider.CreateScope();
        var store = new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
        var fileId = Guid.NewGuid();
        var hash = new byte[] { 1, 2, 3, 4, 5 };
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, hash), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var shortHash = new byte[] { 1, 2, 3 };
        var result = await store.FindByHashAsync(shortHash, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Null(result);
    }
}