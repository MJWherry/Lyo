using System.Security.Cryptography;
using System.Text;
using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Drift.Models;
using Lyo.Drift.Postgres;
using Lyo.Drift.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Drift.Seed;

/// <summary>EF seed of stopped drift instances, one structure snapshot, one diff, and one change event each. Schema <c>drift</c>.</summary>
public sealed class DriftEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Drift";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresDriftOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<DriftDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new DriftDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var now = DateTime.UtcNow;
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var snapshotIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        var index = new Dictionary<Guid, int>();
        var hosts = new Dictionary<Guid, string>();
        var instances = graph.Entity(
            options.Count, i => {
                var host = $"{faker.Internet.DomainWord()}-{i}";
                var instance = new DriftInstance {
                    Id = LyoGuid.CreateV7(),
                    InstanceKey = host,
                    MachineName = host,
                    ProcessId = 1,
                    State = nameof(DriftInstanceState.Stopped),
                    LastHeartbeatUtc = now,
                    CreatedTimestamp = now,
                    WatchesJson = "[]",
                    MetadataJson = "{}"
                };

                index[instance.Id] = i;
                hosts[instance.Id] = host;
                return instance;
            });

        instances.ForEach(
            _ => 1, (instance, _) => {
                var i = index[instance.Id];
                var root = $"/var/drift/{hosts[instance.Id]}";
                return new DriftStructureSnapshot {
                    Id = snapshotIds[i],
                    InstanceId = instance.Id,
                    Kind = nameof(DriftSnapshotKind.FileTree),
                    WatchRoot = root,
                    TakenAtUtc = now,
                    ReceivedAtUtc = now,
                    ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(root))).ToLowerInvariant(),
                    ContentHashAlgorithm = "Sha256",
                    TreeJson = $"{{\"name\":\"{hosts[instance.Id]}\"}}"
                };
            });

        instances.ForEach(
            _ => 1, (instance, _) => new DriftDiffSnapshot {
                Id = LyoGuid.CreateV7(),
                InstanceId = instance.Id,
                ToSnapshotId = snapshotIds[index[instance.Id]],
                Source = nameof(DriftDiffSource.Server),
                ComputedAtUtc = now,
                FileChangesJson = "[]"
            });

        instances.ForEach(
            _ => 1, (instance, _) => new DriftChangeEvent {
                Id = LyoGuid.CreateV7(),
                InstanceId = instance.Id,
                WatchRoot = $"/var/drift/{hosts[instance.Id]}",
                SnapshotId = snapshotIds[index[instance.Id]],
                OccurredAtUtc = now,
                ChangeJson = """{"type":"created"}"""
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<DriftDbContext> ef)
            throw new SeedException("Drift OnClear requires EfSeedTransport<DriftDbContext>.");

        var db = ef.Context;
        await db.Changes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Diffs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Snapshots.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Instances.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}