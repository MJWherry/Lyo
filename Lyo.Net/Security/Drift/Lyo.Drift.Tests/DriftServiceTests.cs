using Lyo.Common.Json;
using Lyo.Drift.Models;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Postgres;
using Lyo.FileSystemWatcher.Models;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Lyo.Drift.Tests;

[Trait("Category", "Integration")]
public sealed class DriftServiceTests
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();
    private readonly DriftPostgresFixture _fixture;

    public DriftServiceTests(DriftPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Upsert_SameKey_ReusesId()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = _fixture.CreateScope();
        var drift = scope.ServiceProvider.GetRequiredService<DriftService>();
        var first = await drift.UpsertInstanceAsync(new() { InstanceKey = "svc-key", MachineName = "a", ProcessId = 1 }, ct);
        var second = await drift.UpsertInstanceAsync(new() { InstanceKey = "svc-key", MachineName = "b", ProcessId = 2 }, ct);
        second.Id.ShouldBe(first.Id);
        second.ProcessId.ShouldBe(2);
    }

    [Fact]
    public async Task SaveSnapshot_SameHash_Dedupes()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = _fixture.CreateScope();
        var drift = scope.ServiceProvider.GetRequiredService<DriftService>();
        var instance = await drift.UpsertInstanceAsync(new() { InstanceKey = "svc-dedupe", MachineName = "a", ProcessId = 1 }, ct);
        var tree = new FileSystemSnapshotTreeDto { RootPath = "/tmp/d", Root = new() };
        var json = JsonSerializer.Serialize(tree, Json);
        var first = await drift.SaveSnapshotAsync(
            new() {
                InstanceId = instance.Id,
                Kind = DriftSnapshotKind.FileTree,
                WatchRoot = "/tmp/d",
                TakenAtUtc = DateTime.UtcNow,
                TreeJson = json
            }, ct);
        var second = await drift.SaveSnapshotAsync(
            new() {
                InstanceId = instance.Id,
                Kind = DriftSnapshotKind.FileTree,
                WatchRoot = "/tmp/d",
                TakenAtUtc = DateTime.UtcNow.AddSeconds(1),
                TreeJson = json
            }, ct);
        second.Id.ShouldBe(first.Id);
        second.Deduped.ShouldBeTrue();
    }

    [Fact]
    public async Task DiffAgainst_FileTree_UsesDtoDetectChanges()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = _fixture.CreateScope();
        var drift = scope.ServiceProvider.GetRequiredService<DriftService>();
        var instance = await drift.UpsertInstanceAsync(new() { InstanceKey = "svc-diff", MachineName = "a", ProcessId = 1 }, ct);
        var oldTree = new FileSystemSnapshotTreeDto { RootPath = "/tmp/x", Root = new() };
        var newTree = new FileSystemSnapshotTreeDto {
            RootPath = "/tmp/x",
            FileCount = 1,
            Root = new() { Files = { ["n.txt"] = new() { Path = "n.txt", Hash = "ab" } } }
        };
        var a = await drift.SaveSnapshotAsync(
            new() {
                InstanceId = instance.Id,
                Kind = DriftSnapshotKind.FileTree,
                WatchRoot = "/tmp/x",
                TakenAtUtc = DateTime.UtcNow,
                TreeJson = JsonSerializer.Serialize(oldTree, Json)
            }, ct);
        var b = await drift.SaveSnapshotAsync(
            new() {
                InstanceId = instance.Id,
                Kind = DriftSnapshotKind.FileTree,
                WatchRoot = "/tmp/x",
                TakenAtUtc = DateTime.UtcNow.AddSeconds(1),
                TreeJson = JsonSerializer.Serialize(newTree, Json)
            }, ct);
        var diff = await drift.DiffAgainstAsync(a.Id, b.Id, ct);
        diff.Source.ShouldBe(DriftDiffSource.Server);
        diff.FileChangesJson.ShouldNotBeNull();
        diff.FileChangesJson.ShouldContain("n.txt");
    }

    [Fact]
    public async Task Prune_KeepsLatestPerLineage()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = _fixture.CreateScope();
        var drift = scope.ServiceProvider.GetRequiredService<DriftService>();
        var instance = await drift.UpsertInstanceAsync(new() { InstanceKey = "svc-prune", MachineName = "a", ProcessId = 1 }, ct);
        var oldSnap = await drift.SaveSnapshotAsync(
            new() {
                InstanceId = instance.Id,
                Kind = DriftSnapshotKind.FileTree,
                WatchRoot = "/tmp/p",
                TakenAtUtc = DateTime.UtcNow.AddDays(-10),
                TreeJson = "{\"rootPath\":\"/tmp/p\",\"fileCount\":0}"
            }, ct);
        var latest = await drift.SaveSnapshotAsync(
            new() {
                InstanceId = instance.Id,
                Kind = DriftSnapshotKind.FileTree,
                WatchRoot = "/tmp/p",
                TakenAtUtc = DateTime.UtcNow.AddDays(-9),
                TreeJson = "{\"rootPath\":\"/tmp/p\",\"fileCount\":1}"
            }, ct);
        var deleted = await drift.PruneAsync(ct);
        deleted.ShouldBeGreaterThan(0);
        var kept = await drift.GetSnapshotAsync(latest.Id, ct);
        kept.ShouldNotBeNull();
        var gone = await drift.GetSnapshotAsync(oldSnap.Id, ct);
        gone.ShouldBeNull();
    }
}
