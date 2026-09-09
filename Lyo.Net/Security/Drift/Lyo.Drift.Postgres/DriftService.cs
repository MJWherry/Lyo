using System.Text;
using System.Text.Json;
using Lyo.Api.Models.Error;
using ApiConstants = Lyo.Api.Models.Constants;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Json;
using Lyo.Diff.ObjectGraph;
using Lyo.Drift.Models;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Models.Response;
using Lyo.Drift.Postgres.Database;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Models;
using Lyo.Hashing;
using Microsoft.EntityFrameworkCore;
namespace Lyo.Drift.Postgres;

/// <summary>Ingest and query for drift instances, structure snapshots, diffs, and live changes.</summary>
public sealed class DriftService
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();
    private readonly IDbContextFactory<DriftDbContext> _factory;
    private readonly IObjectGraphDiffService _objectGraphDiff;
    private readonly IHashingService _hashing;
    private readonly PostgresDriftOptions _options;

    /// <summary>Builds a service over <paramref name="factory" />.</summary>
    public DriftService(
        IDbContextFactory<DriftDbContext> factory,
        IObjectGraphDiffService objectGraphDiff,
        IHashingService hashing,
        PostgresDriftOptions options)
    {
        ArgumentHelpers.ThrowIfNull(factory);
        ArgumentHelpers.ThrowIfNull(objectGraphDiff);
        ArgumentHelpers.ThrowIfNull(hashing);
        ArgumentHelpers.ThrowIfNull(options);
        _factory = factory;
        _objectGraphDiff = objectGraphDiff;
        _hashing = hashing;
        _options = options;
    }

    /// <summary>Inserts or updates an instance row keyed by <see cref="DriftInstanceReq.InstanceKey" />.</summary>
    public async Task<DriftInstanceRes> UpsertInstanceAsync(DriftInstanceReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.InstanceKey);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var existing = await db.Instances.FirstOrDefaultAsync(i => i.InstanceKey == request.InstanceKey, ct).ConfigureAwait(false);
        if (existing is null) {
            existing = new DriftInstance {
                Id = LyoGuid.CreateCombPostgres(),
                InstanceKey = request.InstanceKey,
                CreatedTimestamp = now
            };
            db.Instances.Add(existing);
        }

        existing.MachineName = request.MachineName;
        existing.ProcessId = request.ProcessId;
        existing.State = request.State.ToString();
        existing.LastHeartbeatUtc = now;
        existing.UpdatedTimestamp = now;
        existing.WatchesJson = request.WatchesJson;
        existing.MetadataJson = request.Metadata is null ? existing.MetadataJson : JsonSerializer.Serialize(request.Metadata, Json);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToRes(existing);
    }

    /// <summary>Updates heartbeat (and optional state) for <paramref name="instanceId" />.</summary>
    public async Task<DriftInstanceRes> HeartbeatAsync(Guid instanceId, DriftInstanceHeartbeatReq? request, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Instances.FirstOrDefaultAsync(i => i.Id == instanceId, ct).ConfigureAwait(false)
            ?? throw NotFound($"Drift instance '{instanceId}' was not found.");
        var now = DateTime.UtcNow;
        entity.LastHeartbeatUtc = request?.LastHeartbeatUtc == default || request is null ? now : request.LastHeartbeatUtc;
        if (request?.State is { } state)
            entity.State = state.ToString();

        entity.UpdatedTimestamp = now;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToRes(entity);
    }

    /// <summary>Marks the instance Stopped.</summary>
    public async Task<DriftInstanceRes> StopAsync(Guid instanceId, CancellationToken ct = default)
        => await HeartbeatAsync(instanceId, new() { LastHeartbeatUtc = DateTime.UtcNow, State = DriftInstanceState.Stopped }, ct).ConfigureAwait(false);

    /// <summary>Loads one instance.</summary>
    public async Task<DriftInstanceRes?> GetInstanceAsync(Guid instanceId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Instances.AsNoTracking().FirstOrDefaultAsync(i => i.Id == instanceId, ct).ConfigureAwait(false);
        return entity is null ? null : ToRes(entity);
    }

    /// <summary>Stores a structure snapshot. Identical content hash as the latest in the lineage returns that existing id with <see cref="DriftSnapshotRes.Deduped" /> true.</summary>
    public async Task<DriftSnapshotRes> SaveSnapshotAsync(DriftSnapshotReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var payload = request.Kind == DriftSnapshotKind.FileTree ? request.TreeJson : request.SystemInfoJson;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(payload);
        EnsurePayloadSize(payload);
        if (request.Kind == DriftSnapshotKind.FileTree)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.WatchRoot);

        var watchRoot = NormalizeWatchRoot(request.Kind, request.WatchRoot);
        var algorithm = _options.ContentHashAlgorithm;
        var algorithmName = algorithm.ToString();
        var hash = Hash(payload, algorithm);
        var taken = request.TakenAtUtc == default ? DateTime.UtcNow : request.TakenAtUtc;
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await EnsureInstanceExistsAsync(db, request.InstanceId, ct).ConfigureAwait(false);
        var latest = await db.Snapshots.AsNoTracking()
            .Where(s => s.InstanceId == request.InstanceId && s.Kind == request.Kind.ToString() && s.WatchRoot == watchRoot)
            .OrderByDescending(s => s.TakenAtUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (latest != null
            && string.Equals(latest.ContentHash, hash, StringComparison.Ordinal)
            && string.Equals(latest.ContentHashAlgorithm, algorithmName, StringComparison.Ordinal))
            return ToRes(latest, deduped: true);

        var entity = new DriftStructureSnapshot {
            Id = LyoGuid.CreateCombPostgres(),
            InstanceId = request.InstanceId,
            Kind = request.Kind.ToString(),
            WatchRoot = watchRoot,
            TakenAtUtc = taken,
            ReceivedAtUtc = DateTime.UtcNow,
            ContentHash = hash,
            ContentHashAlgorithm = algorithmName,
            TreeJson = request.Kind == DriftSnapshotKind.FileTree ? payload : null,
            SystemInfoJson = request.Kind == DriftSnapshotKind.SystemInfo ? payload : null
        };
        db.Snapshots.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToRes(entity, deduped: false);
    }

    /// <summary>Stores a precomputed diff.</summary>
    public async Task<DriftDiffRes> SaveDiffAsync(DriftDiffReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await EnsureInstanceExistsAsync(db, request.InstanceId, ct).ConfigureAwait(false);
        var entity = new DriftDiffSnapshot {
            Id = LyoGuid.CreateCombPostgres(),
            InstanceId = request.InstanceId,
            FromSnapshotId = request.FromSnapshotId,
            ToSnapshotId = request.ToSnapshotId,
            Source = request.Source.ToString(),
            ComputedAtUtc = DateTime.UtcNow,
            FileChangesJson = request.FileChangesJson,
            SystemDifferencesJson = request.SystemDifferencesJson
        };
        db.Diffs.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToRes(entity);
    }

    /// <summary>Stores a live change batch.</summary>
    public async Task<IReadOnlyList<DriftChangeRes>> SaveChangesAsync(DriftChangeBatchReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(request.Changes);
        if (request.Changes.Count == 0)
            return [];

        var watchRoot = request.WatchRoot ?? "";
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await EnsureInstanceExistsAsync(db, request.InstanceId, ct).ConfigureAwait(false);
        var created = new List<DriftChangeEvent>(request.Changes.Count);
        foreach (var change in request.Changes) {
            var entity = new DriftChangeEvent {
                Id = LyoGuid.CreateCombPostgres(),
                InstanceId = request.InstanceId,
                WatchRoot = watchRoot,
                OccurredAtUtc = change.OccurredAtUtc == default ? DateTime.UtcNow : change.OccurredAtUtc,
                ChangeJson = JsonSerializer.Serialize(change, Json)
            };
            db.Changes.Add(entity);
            created.Add(entity);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return created.Select(ToRes).ToArray();
    }

    /// <summary>Loads one stored snapshot.</summary>
    public async Task<DriftSnapshotRes?> GetSnapshotAsync(Guid snapshotId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Snapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == snapshotId, ct).ConfigureAwait(false);
        return entity is null ? null : ToRes(entity, deduped: false);
    }

    /// <summary>Loads one stored diff.</summary>
    public async Task<DriftDiffRes?> GetDiffAsync(Guid diffId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Diffs.AsNoTracking().FirstOrDefaultAsync(d => d.Id == diffId, ct).ConfigureAwait(false);
        return entity is null ? null : ToRes(entity);
    }

    /// <summary>
    /// Diffs two stored snapshots of the same kind. File trees use DTO <see cref="FileSystemSnapshotDiffer.DetectChanges" />; system-info uses
    /// <see cref="IObjectGraphDiffService" /> on <see cref="SystemInfoDriftProjection" />.
    /// </summary>
    public async Task<DriftDiffRes> DiffAgainstAsync(Guid snapshotId, Guid otherId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var left = await db.Snapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == snapshotId, ct).ConfigureAwait(false)
            ?? throw NotFound($"Snapshot '{snapshotId}' was not found.");
        var right = await db.Snapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == otherId, ct).ConfigureAwait(false)
            ?? throw NotFound($"Snapshot '{otherId}' was not found.");
        if (!string.Equals(left.Kind, right.Kind, StringComparison.Ordinal))
            throw Invalid($"Snapshots must be the same kind (got {left.Kind} and {right.Kind}).");

        string? fileChangesJson = null;
        string? systemDifferencesJson = null;
        if (string.Equals(left.Kind, nameof(DriftSnapshotKind.FileTree), StringComparison.Ordinal)) {
            var oldTree = DeserializeRequired<FileSystemSnapshotTreeDto>(left.TreeJson, "file-tree snapshot");
            var newTree = DeserializeRequired<FileSystemSnapshotTreeDto>(right.TreeJson, "file-tree snapshot");
            var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, newTree, DateTime.UtcNow, ct);
            fileChangesJson = JsonSerializer.Serialize(changes, Json);
        }
        else {
            var oldProjection = DeserializeRequired<SystemInfoDriftProjection>(left.SystemInfoJson, "system-info snapshot");
            var newProjection = DeserializeRequired<SystemInfoDriftProjection>(right.SystemInfoJson, "system-info snapshot");
            var diffs = _objectGraphDiff.GetDifferences(oldProjection, newProjection);
            var dtos = diffs.Select(d => new ObjectGraphDifferenceDto {
                Path = d.Path,
                OldValueJson = d.OldValue is null ? null : JsonSerializer.Serialize(d.OldValue, Json),
                NewValueJson = d.NewValue is null ? null : JsonSerializer.Serialize(d.NewValue, Json)
            }).ToArray();
            systemDifferencesJson = JsonSerializer.Serialize(dtos, Json);
        }

        var entity = new DriftDiffSnapshot {
            Id = LyoGuid.CreateCombPostgres(),
            InstanceId = right.InstanceId,
            FromSnapshotId = left.Id,
            ToSnapshotId = right.Id,
            Source = nameof(DriftDiffSource.Server),
            ComputedAtUtc = DateTime.UtcNow,
            FileChangesJson = fileChangesJson,
            SystemDifferencesJson = systemDifferencesJson
        };
        db.Diffs.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToRes(entity);
    }

    /// <summary>Deletes snapshots and change events older than <see cref="PostgresDriftOptions.SnapshotRetention" />, keeping the latest row per lineage.</summary>
    public async Task<int> PruneAsync(CancellationToken ct = default)
    {
        if (_options.SnapshotRetention is not { } retention)
            return 0;

        var cutoff = DateTime.UtcNow - retention;
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var latestIds = await db.Snapshots.AsNoTracking()
            .GroupBy(s => new { s.InstanceId, s.Kind, s.WatchRoot })
            .Select(g => g.OrderByDescending(s => s.TakenAtUtc).Select(s => s.Id).First())
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var keep = latestIds.ToHashSet();
        var oldSnapshots = await db.Snapshots.Where(s => s.TakenAtUtc < cutoff && !keep.Contains(s.Id)).ToListAsync(ct).ConfigureAwait(false);
        var removedIds = oldSnapshots.Select(s => s.Id).ToHashSet();
        if (removedIds.Count > 0) {
            var orphanDiffs = await db.Diffs
                .Where(d => (d.FromSnapshotId != null && removedIds.Contains(d.FromSnapshotId.Value)) || removedIds.Contains(d.ToSnapshotId))
                .ToListAsync(ct)
                .ConfigureAwait(false);
            db.Diffs.RemoveRange(orphanDiffs);
            db.Snapshots.RemoveRange(oldSnapshots);
        }

        var oldChanges = await db.Changes.Where(c => c.OccurredAtUtc < cutoff).ToListAsync(ct).ConfigureAwait(false);
        db.Changes.RemoveRange(oldChanges);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return oldSnapshots.Count + oldChanges.Count;
    }

    private string Hash(string json, ContentDigestAlgorithm algorithm)
    {
        var digest = _hashing.Hash(algorithm, Encoding.UTF8.GetBytes(json));
        return _hashing.ToHex(digest, TextLetterCase.Lower);
    }

    private void EnsurePayloadSize(string json)
    {
        var bytes = Encoding.UTF8.GetByteCount(json);
        if (bytes > _options.MaxSnapshotJsonBytes) {
            throw ApiErrorException.From(
                new LyoProblemDetails(
                    $"Snapshot JSON is {bytes} bytes; MaxSnapshotJsonBytes is {_options.MaxSnapshotJsonBytes}.", 413, DateTime.UtcNow,
                    [new(ApiConstants.ApiErrorCodes.UnprocessableEntity, "Snapshot JSON exceeds MaxSnapshotJsonBytes.")]));
        }
    }

    private static string NormalizeWatchRoot(DriftSnapshotKind kind, string? watchRoot)
        => kind == DriftSnapshotKind.SystemInfo ? "" : watchRoot ?? "";

    private static async Task EnsureInstanceExistsAsync(DriftDbContext db, Guid instanceId, CancellationToken ct)
    {
        var exists = await db.Instances.AsNoTracking().AnyAsync(i => i.Id == instanceId, ct).ConfigureAwait(false);
        if (!exists)
            throw NotFound($"Drift instance '{instanceId}' was not found.");
    }

    private static T DeserializeRequired<T>(string? json, string name)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw Invalid($"Stored {name} JSON is empty.");

        return JsonSerializer.Deserialize<T>(json, Json) ?? throw Invalid($"Stored {name} JSON could not be deserialized.");
    }

    private static ApiErrorException NotFound(string detail)
        => ApiErrorException.From(LyoProblemDetails.FromCode(ApiConstants.ApiErrorCodes.NotFound, detail));

    private static ApiErrorException Invalid(string detail)
        => ApiErrorException.From(LyoProblemDetails.FromCode(ApiConstants.ApiErrorCodes.InvalidRequest, detail));

    internal static DriftInstanceRes ToRes(DriftInstance e)
        => new() {
            Id = e.Id,
            InstanceKey = e.InstanceKey,
            MachineName = e.MachineName,
            ProcessId = e.ProcessId,
            State = Enum.TryParse<DriftInstanceState>(e.State, out var state) ? state : DriftInstanceState.Running,
            LastHeartbeatUtc = e.LastHeartbeatUtc,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp,
            WatchesJson = e.WatchesJson,
            MetadataJson = e.MetadataJson
        };

    internal static DriftSnapshotRes ToRes(DriftStructureSnapshot e, bool deduped)
        => new() {
            Id = e.Id,
            InstanceId = e.InstanceId,
            Kind = Enum.TryParse<DriftSnapshotKind>(e.Kind, out var kind) ? kind : DriftSnapshotKind.FileTree,
            WatchRoot = string.IsNullOrEmpty(e.WatchRoot) ? null : e.WatchRoot,
            TakenAtUtc = e.TakenAtUtc,
            ReceivedAtUtc = e.ReceivedAtUtc,
            ContentHash = e.ContentHash,
            ContentHashAlgorithm = e.ContentHashAlgorithm,
            Deduped = deduped,
            TreeJson = e.TreeJson,
            SystemInfoJson = e.SystemInfoJson
        };

    internal static DriftDiffRes ToRes(DriftDiffSnapshot e)
        => new() {
            Id = e.Id,
            InstanceId = e.InstanceId,
            FromSnapshotId = e.FromSnapshotId,
            ToSnapshotId = e.ToSnapshotId,
            Source = Enum.TryParse<DriftDiffSource>(e.Source, out var source) ? source : DriftDiffSource.Agent,
            ComputedAtUtc = e.ComputedAtUtc,
            FileChangesJson = e.FileChangesJson,
            SystemDifferencesJson = e.SystemDifferencesJson
        };

    internal static DriftChangeRes ToRes(DriftChangeEvent e)
        => new() {
            Id = e.Id,
            InstanceId = e.InstanceId,
            WatchRoot = string.IsNullOrEmpty(e.WatchRoot) ? null : e.WatchRoot,
            SnapshotId = e.SnapshotId,
            OccurredAtUtc = e.OccurredAtUtc,
            Change = JsonSerializer.Deserialize<FileSystemChangeDto>(e.ChangeJson, Json) ?? new()
        };
}
