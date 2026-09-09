using System.Text;
using System.Text.Json;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Json;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Models;
using Lyo.FileSystemWatcher.Postgres.Database;
using Lyo.Hashing;
using Microsoft.EntityFrameworkCore;
namespace Lyo.FileSystemWatcher.Postgres;

/// <summary>PostgreSQL implementation of <see cref="IFileSystemWatcherStore" />.</summary>
public sealed class PostgresFileSystemWatcherStore : IFileSystemWatcherStore
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();
    private readonly IDbContextFactory<FileSystemWatcherDbContext> _factory;
    private readonly IHashingService _hashing;
    private readonly PostgresFileSystemWatcherOptions _options;

    /// <summary>Builds a store over <paramref name="factory" />.</summary>
    public PostgresFileSystemWatcherStore(
        IDbContextFactory<FileSystemWatcherDbContext> factory,
        IHashingService hashing,
        PostgresFileSystemWatcherOptions options)
    {
        ArgumentHelpers.ThrowIfNull(factory);
        ArgumentHelpers.ThrowIfNull(hashing);
        ArgumentHelpers.ThrowIfNull(options);
        _factory = factory;
        _hashing = hashing;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateWatchAsync(string rootPath, FileSystemWatchOptionsDto options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentHelpers.ThrowIfNull(options);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = new FileSystemWatchEntity {
            Id = LyoGuid.CreateCombPostgres(),
            RootPath = rootPath,
            OptionsJson = JsonSerializer.Serialize(options, Json),
            CreatedTimestamp = DateTime.UtcNow
        };
        db.Watches.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task<Guid> SaveSnapshotAsync(Guid watchId, FileSystemSnapshotTreeDto tree, DateTime takenAtUtc, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(tree);
        var treeJson = JsonSerializer.Serialize(tree, Json);
        var algorithm = _options.ContentHashAlgorithm;
        var algorithmName = algorithm.ToString();
        var hash = Hash(treeJson, algorithm);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var latest = await db.Snapshots.AsNoTracking()
            .Where(s => s.WatchId == watchId)
            .OrderByDescending(s => s.TakenAtUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (latest != null
            && string.Equals(latest.ContentHash, hash, StringComparison.Ordinal)
            && string.Equals(latest.ContentHashAlgorithm, algorithmName, StringComparison.Ordinal))
            return latest.Id;

        var entity = new FileSystemSnapshotEntity {
            Id = LyoGuid.CreateCombPostgres(),
            WatchId = watchId,
            TakenAtUtc = takenAtUtc == default ? DateTime.UtcNow : takenAtUtc,
            FileCount = tree.FileCount,
            DirectoryCount = tree.DirectoryCount,
            ContentHash = hash,
            ContentHashAlgorithm = algorithmName,
            TreeJson = treeJson
        };
        db.Snapshots.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(Guid watchId, Guid? snapshotId, IReadOnlyList<FileSystemChangeDto> changes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(changes);
        if (changes.Count == 0)
            return;

        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        foreach (var change in changes) {
            db.Changes.Add(
                new() {
                    Id = LyoGuid.CreateCombPostgres(),
                    WatchId = watchId,
                    SnapshotId = snapshotId,
                    OccurredAtUtc = change.OccurredAtUtc == default ? DateTime.UtcNow : change.OccurredAtUtc,
                    ChangeType = change.ChangeType.ToString(),
                    IsDirectory = change.IsDirectory,
                    OldPath = Truncate(change.OldPath, 2048),
                    NewPath = Truncate(change.NewPath, 2048),
                    ChangeJson = JsonSerializer.Serialize(change, Json)
                });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileSystemSnapshotTreeDto?> GetLatestSnapshotAsync(Guid watchId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var json = await db.Snapshots.AsNoTracking()
            .Where(s => s.WatchId == watchId)
            .OrderByDescending(s => s.TakenAtUtc)
            .Select(s => s.TreeJson)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return json is null ? null : JsonSerializer.Deserialize<FileSystemSnapshotTreeDto>(json, Json);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileSystemChangeDto>> GetChangesAsync(Guid watchId, int take = 100, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNegative(take);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await db.Changes.AsNoTracking()
            .Where(c => c.WatchId == watchId)
            .OrderByDescending(c => c.OccurredAtUtc)
            .Take(take)
            .Select(c => c.ChangeJson)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var list = new List<FileSystemChangeDto>(rows.Count);
        foreach (var json in rows) {
            var dto = JsonSerializer.Deserialize<FileSystemChangeDto>(json, Json);
            if (dto != null)
                list.Add(dto);
        }

        return list;
    }

    private string Hash(string json, ContentDigestAlgorithm algorithm)
    {
        var digest = _hashing.Hash(algorithm, Encoding.UTF8.GetBytes(json));
        return _hashing.ToHex(digest, TextLetterCase.Lower);
    }

    private static string? Truncate(string? value, int max)
        => value is null || value.Length <= max ? value : value.Substring(0, max);
}
