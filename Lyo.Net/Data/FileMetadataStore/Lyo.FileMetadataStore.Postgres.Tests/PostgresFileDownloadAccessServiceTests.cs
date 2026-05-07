using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Postgres.Tests;

public class PostgresFileDownloadAccessServiceTests
{
    private readonly FileMetadataPostgresFixture _fixture;

    public PostgresFileDownloadAccessServiceTests(FileMetadataPostgresFixture fixture) => _fixture = fixture;

    private static FileStoreResult CreateMetadata(Guid id, byte[] hash)
        => new(
            id, "original.txt", 128, hash, "source.txt", 128, hash, false, null, null, null, false, null, null, null, null, null, null, null, null, DateTime.UtcNow, null,
            HashAlgorithm.Sha256, "text/plain");

    [Fact]
    public async Task CreateLinkAndConsume_AllowsDownload()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        var store = new PostgresFileMetadataStore(db);
        var service = scope.ServiceProvider.GetRequiredService<IFileDownloadAccessService>();
        var fileId = Guid.NewGuid();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [1, 2, 3, 4]), TestContext.Current.CancellationToken);
        var created = await service.CreateLinkAsync(new(fileId, MaxDownloads: 5), TestContext.Current.CancellationToken);
        var consumed = await service.ValidateAndConsumeDownloadAsync(created.Token, nowUtc: DateTime.UtcNow, ct: TestContext.Current.CancellationToken);
        Assert.True(consumed.IsAllowed);
        Assert.Equal(fileId, consumed.FileId);
        Assert.Equal(1, consumed.DownloadCount);
    }

    [Fact]
    public async Task ValidateAndConsume_NotBeforeInFuture_Denies()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        var store = new PostgresFileMetadataStore(db);
        var service = scope.ServiceProvider.GetRequiredService<IFileDownloadAccessService>();
        var fileId = Guid.NewGuid();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [5, 6, 7, 8]), TestContext.Current.CancellationToken);
        var now = DateTime.UtcNow;
        var created = await service.CreateLinkAsync(new(fileId, now.AddMinutes(5)), TestContext.Current.CancellationToken);
        var consumed = await service.ValidateAndConsumeDownloadAsync(created.Token, nowUtc: now, ct: TestContext.Current.CancellationToken);
        Assert.False(consumed.IsAllowed);
        Assert.Equal(FileDownloadAccessConsumeFailureReason.NotYetValid, consumed.FailureReason);
    }

    [Fact]
    public async Task ValidateAndConsume_MaxDownloads_EnforcedUnderConcurrency()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        var store = new PostgresFileMetadataStore(db);
        var service = scope.ServiceProvider.GetRequiredService<IFileDownloadAccessService>();
        var fileId = Guid.NewGuid();
        await store.SaveMetadataAsync(fileId, CreateMetadata(fileId, [9, 10, 11, 12]), TestContext.Current.CancellationToken);
        var created = await service.CreateLinkAsync(new(fileId, MaxDownloads: 5), TestContext.Current.CancellationToken);
        var tasks = Enumerable.Range(0, 20).Select(_ => service.ValidateAndConsumeDownloadAsync(created.Token, nowUtc: DateTime.UtcNow, ct: TestContext.Current.CancellationToken));
        var results = await Task.WhenAll(tasks);
        var allowedCount = results.Count(r => r.IsAllowed);
        var deniedCount = results.Count(r => r.FailureReason == FileDownloadAccessConsumeFailureReason.MaxDownloadsReached);
        Assert.Equal(5, allowedCount);
        Assert.True(deniedCount >= 15);
        var link = await db.FileDownloadAccessLinks.AsNoTracking().SingleAsync(i => i.Id == created.LinkId, TestContext.Current.CancellationToken);
        Assert.Equal(5, link.DownloadCount);
    }
}