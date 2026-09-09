using Lyo.FileSystemWatcher.Models;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileSystemWatcher.Postgres.Tests;

[Trait("Category", "Integration")]
public sealed class FileSystemWatcherStoreTests
{
    private readonly FileSystemWatcherPostgresFixture _fixture;

    public FileSystemWatcherStoreTests(FileSystemWatcherPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SaveSnapshot_ThenGetLatest_RoundTripsTree()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = _fixture.ServiceProvider.GetRequiredService<IFileSystemWatcherStore>();
        var watchId = await store.CreateWatchAsync("/tmp/watch", new FileSystemWatchOptionsDto { IncludeSubdirectories = true }, ct);
        var tree = new FileSystemSnapshotTreeDto {
            RootPath = "/tmp/watch",
            PathComparison = nameof(StringComparison.OrdinalIgnoreCase),
            FileCount = 1,
            Root = new() {
                Files = { ["a.txt"] = new() { Path = "a.txt", FileSize = 4, Hash = "abcd" } }
            }
        };
        var snapshotId = await store.SaveSnapshotAsync(watchId, tree, DateTime.UtcNow, ct);
        snapshotId.ShouldNotBe(Guid.Empty);
        var loaded = await store.GetLatestSnapshotAsync(watchId, ct);
        loaded.ShouldNotBeNull();
        loaded!.FileCount.ShouldBe(1);
        loaded.EnumerateFiles().Single().Path.ShouldBe("a.txt");
    }

    [Fact]
    public async Task SaveSnapshot_SameHash_ReturnsExistingId()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = _fixture.ServiceProvider.GetRequiredService<IFileSystemWatcherStore>();
        var watchId = await store.CreateWatchAsync("/tmp/dedupe", new FileSystemWatchOptionsDto(), ct);
        var tree = new FileSystemSnapshotTreeDto { RootPath = "/tmp/dedupe", FileCount = 0, Root = new() };
        var first = await store.SaveSnapshotAsync(watchId, tree, DateTime.UtcNow, ct);
        var second = await store.SaveSnapshotAsync(watchId, tree, DateTime.UtcNow.AddSeconds(1), ct);
        second.ShouldBe(first);
    }

    [Fact]
    public async Task SaveChanges_CanBeReadBack()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = _fixture.ServiceProvider.GetRequiredService<IFileSystemWatcherStore>();
        var watchId = await store.CreateWatchAsync("/tmp/changes", new FileSystemWatchOptionsDto(), ct);
        await store.SaveChangesAsync(
            watchId, null, [
                new() {
                    NewPath = "a.txt",
                    ChangeType = FileSystemChangeKind.Created,
                    OccurredAtUtc = DateTime.UtcNow
                }
            ], ct);
        var changes = await store.GetChangesAsync(watchId, 10, ct);
        changes.ShouldHaveCount(1);
        changes[0].ChangeType.ShouldBe(FileSystemChangeKind.Created);
        changes[0].NewPath.ShouldBe("a.txt");
    }
}
