using System.Collections.Concurrent;
using Lyo.Api.Models.Common.Response;
using Lyo.Diff;
using Lyo.Drift.Agent;
using Lyo.Drift.Client;
using Lyo.Drift.Models;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Models.Response;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
namespace Lyo.Drift.Tests;

public sealed class DriftAgentTests
{
    [Fact]
    public async Task ScanCompleted_DoesNotPostOnDebounceThread_AndHonorsIncludeExclude()
    {
        var dir = Directory.CreateTempSubdirectory("lyo-drift-agent-");
        try {
            var fake = new FakeDriftClient();
            var services = new ServiceCollection();
            services.AddLyoDiff();
            var sp = services.BuildServiceProvider();
            var options = new DriftAgentOptions {
                InstanceKey = "agent-test",
                CollectSystemInfo = false,
                HeartbeatInterval = TimeSpan.FromHours(1),
                SystemInfoInterval = TimeSpan.FromHours(1),
                Watches = [
                    new() {
                        Path = dir.FullName,
                        IncludeSubdirectories = false,
                        IncludePatterns = [@"\.txt$"],
                        ExcludePatterns = [@"^secret\.txt$"],
                        EnableFileHashing = false,
                        DebounceTimerDelay = 50
                    }
                ]
            };
            var agent = new DriftAgentHostedService(options, fake, sp.GetRequiredService<Lyo.Diff.ObjectGraph.IObjectGraphDiffService>(), NullLogger<DriftAgentHostedService>.Instance);
            fake.Agent = agent;
            await agent.StartAsync(TestContext.Current.CancellationToken);
            await PollAssert.ThatAsync(() => fake.Snapshots.Count >= 1, TimeSpan.FromSeconds(10));
            var afterStart = fake.Snapshots.Count;
            await File.WriteAllTextAsync(Path.Combine(dir.FullName, "hello.txt"), "hi", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir.FullName, "secret.txt"), "nope", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir.FullName, "skip.bin"), "bin", TestContext.Current.CancellationToken);
            await PollAssert.ThatAsync(() => fake.Snapshots.Count > afterStart, TimeSpan.FromSeconds(10));
            agent.LastScanCompletedThreadId.ShouldNotBeNull();
            fake.PostedDuringScanCompleted.ShouldBeFalse();
            fake.Snapshots.ShouldAllSatisfy(s => s.TreeJson is null || (!s.TreeJson.Contains("secret.txt", StringComparison.Ordinal) && !s.TreeJson.Contains("skip.bin", StringComparison.Ordinal)));
            await agent.StopAsync(CancellationToken.None);
        }
        finally {
            try {
                dir.Delete(true);
            }
            catch (IOException) { }
        }
    }

    [Fact]
    public async Task QueueSystemInfo_DoesNotPostOnTimerThread()
    {
        var fake = new FakeDriftClient();
        var services = new ServiceCollection();
        services.AddLyoDiff();
        var sp = services.BuildServiceProvider();
        var options = new DriftAgentOptions {
            InstanceKey = "agent-sys",
            CollectSystemInfo = true,
            HeartbeatInterval = TimeSpan.FromHours(1),
            SystemInfoInterval = TimeSpan.FromHours(1),
            Watches = []
        };
        var agent = new DriftAgentHostedService(options, fake, sp.GetRequiredService<Lyo.Diff.ObjectGraph.IObjectGraphDiffService>(), NullLogger<DriftAgentHostedService>.Instance);
        fake.Agent = agent;
        await agent.StartAsync(TestContext.Current.CancellationToken);
        await PollAssert.ThatAsync(() => fake.Snapshots.Any(s => s.Kind == DriftSnapshotKind.SystemInfo), TimeSpan.FromSeconds(10));
        var count = fake.SnapshotCalls.Count(c => c.Kind == DriftSnapshotKind.SystemInfo);
        agent.QueueSystemInfo();
        await PollAssert.ThatAsync(() => fake.SnapshotCalls.Count(c => c.Kind == DriftSnapshotKind.SystemInfo) > count, TimeSpan.FromSeconds(10));
        fake.PostedDuringSystemInfoTimer.ShouldBeFalse();
        await agent.StopAsync(CancellationToken.None);
    }
}

internal sealed class FakeDriftClient : IDriftClient
{
    private readonly ConcurrentDictionary<Guid, DriftSnapshotRes> _byId = new();
    public DriftAgentHostedService? Agent { get; set; }
    public bool PostedDuringScanCompleted { get; private set; }
    public bool PostedDuringSystemInfoTimer { get; private set; }
    public ConcurrentBag<DriftSnapshotRes> Snapshots { get; } = [];
    public ConcurrentQueue<(DriftSnapshotKind Kind, int ThreadId, string? Json)> SnapshotCalls { get; } = [];

    public Task<CreateResult<DriftInstanceRes>> UpsertInstanceAsync(DriftInstanceReq request, CancellationToken ct = default)
        => Task.FromResult(ResultFactory.CreateSuccess(new DriftInstanceRes { Id = Guid.NewGuid(), InstanceKey = request.InstanceKey }));

    public Task<DriftInstanceRes> HeartbeatAsync(Guid instanceId, DriftInstanceHeartbeatReq? request = null, CancellationToken ct = default)
        => Task.FromResult(new DriftInstanceRes { Id = instanceId });

    public Task<DriftInstanceRes> StopAsync(Guid instanceId, CancellationToken ct = default)
        => Task.FromResult(new DriftInstanceRes { Id = instanceId, State = DriftInstanceState.Stopped });

    public Task<CreateResult<DriftSnapshotRes>> PostSnapshotAsync(DriftSnapshotReq request, CancellationToken ct = default)
    {
        if (Agent?.ScanCompletedHandlerRunning == true)
            PostedDuringScanCompleted = true;
        if (Agent?.SystemInfoTimerCallbackRunning == true)
            PostedDuringSystemInfoTimer = true;
        var json = request.TreeJson ?? request.SystemInfoJson ?? "";
        SnapshotCalls.Enqueue((request.Kind, Environment.CurrentManagedThreadId, json));
        var existing = Snapshots.LastOrDefault(s => s.Kind == request.Kind && s.WatchRoot == request.WatchRoot && (s.TreeJson == request.TreeJson && s.SystemInfoJson == request.SystemInfoJson));
        if (existing is not null) {
            var dup = new DriftSnapshotRes {
                Id = existing.Id,
                InstanceId = request.InstanceId,
                Kind = request.Kind,
                WatchRoot = request.WatchRoot,
                ContentHash = existing.ContentHash,
                ContentHashAlgorithm = existing.ContentHashAlgorithm,
                Deduped = true,
                TreeJson = request.TreeJson,
                SystemInfoJson = request.SystemInfoJson
            };
            return Task.FromResult(ResultFactory.CreateSuccess(dup));
        }

        var res = new DriftSnapshotRes {
            Id = Guid.NewGuid(),
            InstanceId = request.InstanceId,
            Kind = request.Kind,
            WatchRoot = request.WatchRoot,
            TakenAtUtc = request.TakenAtUtc,
            ReceivedAtUtc = DateTime.UtcNow,
            ContentHash = Guid.NewGuid().ToString("N"),
            ContentHashAlgorithm = "Sha256",
            Deduped = false,
            TreeJson = request.TreeJson,
            SystemInfoJson = request.SystemInfoJson
        };
        Snapshots.Add(res);
        _byId[res.Id] = res;
        return Task.FromResult(ResultFactory.CreateSuccess(res));
    }

    public Task<CreateResult<DriftDiffRes>> PostDiffAsync(DriftDiffReq request, CancellationToken ct = default)
        => Task.FromResult(ResultFactory.CreateSuccess(new DriftDiffRes { Id = Guid.NewGuid(), InstanceId = request.InstanceId, ToSnapshotId = request.ToSnapshotId }));

    public Task<IReadOnlyList<DriftChangeRes>> PostChangesAsync(DriftChangeBatchReq request, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DriftChangeRes>>([]);

    public Task<DriftSnapshotRes?> GetSnapshotAsync(Guid snapshotId, CancellationToken ct = default)
        => Task.FromResult(_byId.TryGetValue(snapshotId, out var s) ? s : null);

    public Task<DriftInstanceRes?> GetInstanceAsync(Guid instanceId, CancellationToken ct = default)
        => Task.FromResult<DriftInstanceRes?>(new() { Id = instanceId });

    public Task<CreateResult<DriftDiffRes>> DiffAgainstAsync(Guid snapshotId, Guid otherId, CancellationToken ct = default)
        => Task.FromResult(ResultFactory.CreateSuccess(new DriftDiffRes { Id = Guid.NewGuid(), FromSnapshotId = snapshotId, ToSnapshotId = otherId }));
}
