using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Json;
using Lyo.Drift.Models;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Models.Response;
using Lyo.FileSystemWatcher.Models;
using Lyo.Query.Models.Common.Request;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Drift.Tests;

[Trait("Category", "Integration")]
public sealed class DriftApiIngestTests
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();
    private readonly DriftApiFixture _fixture;

    public DriftApiIngestTests(DriftApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Upsert_Snapshot_DiffAgainst_AndQuery_Work()
    {
        var ct = TestContext.Current.CancellationToken;
        var upsert = await _fixture.Client.PostAsJsonAsync(
            "/Drift/Instance/Upsert", new DriftInstanceReq { InstanceKey = "api-ingest", MachineName = "test", ProcessId = 1 }, Json, ct);
        upsert.EnsureSuccessStatusCode();
        var instance = (await upsert.Content.ReadFromJsonAsync<CreateResult<DriftInstanceRes>>(Json, ct))!.Data!;
        instance.Id.ShouldNotBe(Guid.Empty);

        var again = await _fixture.Client.PostAsJsonAsync(
            "/Drift/Instance/Upsert", new DriftInstanceReq { InstanceKey = "api-ingest", MachineName = "test", ProcessId = 2 }, Json, ct);
        var instance2 = (await again.Content.ReadFromJsonAsync<CreateResult<DriftInstanceRes>>(Json, ct))!.Data!;
        instance2.Id.ShouldBe(instance.Id);
        instance2.ProcessId.ShouldBe(2);

        var treeA = Tree("a.txt");
        var treeB = Tree("a.txt", "b.txt");
        var snapA = await PostSnapshot(instance.Id, "/tmp/watch", treeA, ct);
        var snapB = await PostSnapshot(instance.Id, "/tmp/watch", treeB, ct);
        snapA.Deduped.ShouldBeFalse();
        snapB.Id.ShouldNotBe(snapA.Id);

        var dup = await PostSnapshot(instance.Id, "/tmp/watch", treeB, ct);
        dup.Id.ShouldBe(snapB.Id);
        dup.Deduped.ShouldBeTrue();

        var sysJson = JsonSerializer.Serialize(new SystemInfoDriftProjection { HostName = "h", OsDescription = "os" }, Json);
        var sysReq = new DriftSnapshotReq {
            InstanceId = instance.Id,
            Kind = DriftSnapshotKind.SystemInfo,
            TakenAtUtc = DateTime.UtcNow,
            SystemInfoJson = sysJson
        };
        var sysPost = await _fixture.Client.PostAsJsonAsync("/Drift/Snapshot", sysReq, Json, ct);
        sysPost.EnsureSuccessStatusCode();
        var sysSnap = (await sysPost.Content.ReadFromJsonAsync<CreateResult<DriftSnapshotRes>>(Json, ct))!.Data!;
        sysSnap.Kind.ShouldBe(DriftSnapshotKind.SystemInfo);

        var diffPost = await _fixture.Client.PostAsJsonAsync(
            "/Drift/Diff", new DriftDiffReq {
                InstanceId = instance.Id,
                FromSnapshotId = snapA.Id,
                ToSnapshotId = snapB.Id,
                Source = DriftDiffSource.Agent,
                FileChangesJson = "[]"
            }, Json, ct);
        diffPost.EnsureSuccessStatusCode();

        var changePost = await _fixture.Client.PostAsJsonAsync(
            "/Drift/Change", new DriftChangeBatchReq {
                InstanceId = instance.Id,
                WatchRoot = "/tmp/watch",
                Changes = [new() { NewPath = "b.txt", ChangeType = FileSystemChangeKind.Created, OccurredAtUtc = DateTime.UtcNow }]
            }, Json, ct);
        changePost.EnsureSuccessStatusCode();

        var getSnap = await _fixture.Client.GetAsync($"/Drift/Snapshot/{snapB.Id}", ct);
        getSnap.EnsureSuccessStatusCode();
        var loaded = await getSnap.Content.ReadFromJsonAsync<DriftSnapshotRes>(Json, ct);
        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(snapB.Id);

        var query = await _fixture.Client.PostAsJsonAsync("/Drift/Snapshot/QueryConcrete", new QueryConcreteReq { Start = 0, Amount = 20 }, Json, ct);
        query.EnsureSuccessStatusCode();
        var queried = await query.Content.ReadFromJsonAsync<QueryRes<DriftSnapshotRes>>(Json, ct);
        queried.ShouldNotBeNull();
        queried!.IsSuccess.ShouldBeTrue();
        queried.Items.ShouldNotBeNull();
        queried.Items!.ShouldAnySatisfy(s => s.Id == snapB.Id);

        var against = await _fixture.Client.PostAsJsonAsync($"/Drift/Snapshot/{snapA.Id}/DiffAgainst/{snapB.Id}", new { }, Json, ct);
        against.EnsureSuccessStatusCode();
        var serverDiff = (await against.Content.ReadFromJsonAsync<CreateResult<DriftDiffRes>>(Json, ct))!.Data!;
        serverDiff.Source.ShouldBe(DriftDiffSource.Server);
        serverDiff.FileChangesJson.ShouldNotBeNull();
        serverDiff.FileChangesJson.ShouldContain("b.txt");
    }

    [Fact]
    public async Task Snapshot_OverMaxBytes_IsRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var upsert = await _fixture.Client.PostAsJsonAsync(
            "/Drift/Instance/Upsert", new DriftInstanceReq { InstanceKey = "oversize", MachineName = "test", ProcessId = 1 }, Json, ct);
        var instance = (await upsert.Content.ReadFromJsonAsync<CreateResult<DriftInstanceRes>>(Json, ct))!.Data!;
        var huge = new string('x', 2000);
        var req = new DriftSnapshotReq {
            InstanceId = instance.Id,
            Kind = DriftSnapshotKind.FileTree,
            WatchRoot = "/tmp/huge",
            TakenAtUtc = DateTime.UtcNow,
            TreeJson = $"{{\"rootPath\":\"/tmp/huge\",\"pad\":\"{huge}\"}}"
        };
        var response = await _fixture.Client.PostAsJsonAsync("/Drift/Snapshot", req, Json, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    private async Task<DriftSnapshotRes> PostSnapshot(Guid instanceId, string root, FileSystemSnapshotTreeDto tree, CancellationToken ct)
    {
        var req = new DriftSnapshotReq {
            InstanceId = instanceId,
            Kind = DriftSnapshotKind.FileTree,
            WatchRoot = root,
            TakenAtUtc = DateTime.UtcNow,
            TreeJson = JsonSerializer.Serialize(tree, Json)
        };
        var response = await _fixture.Client.PostAsJsonAsync("/Drift/Snapshot", req, Json, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateResult<DriftSnapshotRes>>(Json, ct))!.Data!;
    }

    private static FileSystemSnapshotTreeDto Tree(params string[] files)
    {
        var root = new FileSystemSnapshotDirectoryDto();
        foreach (var file in files)
            root.Files[file] = new() { Path = file, FileSize = 1, Hash = file };

        return new() { RootPath = "/tmp/watch", FileCount = files.Length, Root = root };
    }
}
