using System.Security.Cryptography;
using Bogus;
using System.Text;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Postgres;
using Lyo.FileSystemWatcher.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.FileSystemWatcher.Seed;

/// <summary>EF seed of watches, one snapshot each, and one change on that snapshot. Schema <c>filesystem_watcher</c>.</summary>
public sealed class FileSystemWatcherEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "File system watcher";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresFileSystemWatcherOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<FileSystemWatcherDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new FileSystemWatcherDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var watches = graph.Entity(options.Count, i => new FileSystemWatchEntity {
            Id = LyoGuid.CreateV7(),
            RootPath = $"/var/watch/{faker.Internet.DomainWord()}-{i}",
            OptionsJson = "{}",
            CreatedTimestamp = now
        });
        watches.ForEach(_ => 1, (watch, _) => new FileSystemSnapshotEntity {
            Id = LyoGuid.CreateV7(),
            WatchId = watch.Id,
            TakenAtUtc = now,
            FileCount = 1,
            DirectoryCount = 1,
            ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(watch.RootPath))).ToLowerInvariant(),
            ContentHashAlgorithm = "Sha256",
            TreeJson = $"{{\"name\":\"{watch.RootPath.Split('/')[^1]}\"}}"
        }).ForEach(_ => 1, (snapshot, _) => new FileSystemChangeEntity {
            Id = LyoGuid.CreateV7(),
            WatchId = snapshot.WatchId,
            SnapshotId = snapshot.Id,
            OccurredAtUtc = now,
            ChangeType = "Created",
            NewPath = snapshot.WatchId.ToString("N") + "/seed.txt",
            ChangeJson = "{}"
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<FileSystemWatcherDbContext> ef)
            throw new SeedException("File system watcher OnClear requires EfSeedTransport<FileSystemWatcherDbContext>.");

        var db = ef.Context;
        await db.Changes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Snapshots.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Watches.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
