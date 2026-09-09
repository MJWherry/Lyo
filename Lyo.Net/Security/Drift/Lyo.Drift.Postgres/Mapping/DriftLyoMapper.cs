using System.Text.Json;
using Lyo.Api.Mapping;
using Lyo.Common.Json;
using Lyo.Drift.Models;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Models.Response;
using Lyo.Drift.Postgres.Database;
using Lyo.FileSystemWatcher.Models;

namespace Lyo.Drift.Postgres.Mapping;

/// <summary>Hand-rolled <see cref="ILyoMapper" /> for Drift Req/entity/Res. Explicit property assignments.</summary>
public sealed class DriftLyoMapper : ILyoMapper
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();

    /// <inheritdoc />
    public TResult Map<TResult>(object source)
        => source switch {
            DriftInstanceReq req when typeof(TResult) == typeof(DriftInstance) => (TResult)(object)ReqToNew(req),
            DriftSnapshotReq req when typeof(TResult) == typeof(DriftStructureSnapshot) => (TResult)(object)ReqToNew(req),
            DriftDiffReq req when typeof(TResult) == typeof(DriftDiffSnapshot) => (TResult)(object)ReqToNew(req),
            DriftChangeBatchReq req when typeof(TResult) == typeof(DriftChangeEvent) => (TResult)(object)ReqToNew(req),
            DriftInstance e when typeof(TResult) == typeof(DriftInstanceRes) => (TResult)(object)DriftService.ToRes(e),
            DriftStructureSnapshot e when typeof(TResult) == typeof(DriftSnapshotRes) => (TResult)(object)DriftService.ToRes(e, false),
            DriftDiffSnapshot e when typeof(TResult) == typeof(DriftDiffRes) => (TResult)(object)DriftService.ToRes(e),
            DriftChangeEvent e when typeof(TResult) == typeof(DriftChangeRes) => (TResult)(object)DriftService.ToRes(e),
            var _ => throw new InvalidOperationException($"No map from {source.GetType().Name} to {typeof(TResult).Name}.")
        };

    /// <inheritdoc />
    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (DriftInstanceReq req, DriftInstance e):
                Apply(req, e);
                break;
            case (DriftSnapshotReq req, DriftStructureSnapshot e):
                Apply(req, e);
                break;
            case (DriftDiffReq req, DriftDiffSnapshot e):
                Apply(req, e);
                break;
            default:
                throw new InvalidOperationException($"No in-place map from {typeof(TSource).Name} to {typeof(TDest).Name}.");
        }
    }

    private static DriftInstance ReqToNew(DriftInstanceReq req)
    {
        var e = new DriftInstance { CreatedTimestamp = DateTime.UtcNow };
        Apply(req, e);
        return e;
    }

    private static void Apply(DriftInstanceReq req, DriftInstance e)
    {
        e.InstanceKey = req.InstanceKey;
        e.MachineName = req.MachineName;
        e.ProcessId = req.ProcessId;
        e.State = req.State.ToString();
        e.WatchesJson = req.WatchesJson;
        if (req.Metadata is not null)
            e.MetadataJson = JsonSerializer.Serialize(req.Metadata, Json);
    }

    private static DriftStructureSnapshot ReqToNew(DriftSnapshotReq req)
    {
        var e = new DriftStructureSnapshot();
        Apply(req, e);
        return e;
    }

    private static void Apply(DriftSnapshotReq req, DriftStructureSnapshot e)
    {
        e.InstanceId = req.InstanceId;
        e.Kind = req.Kind.ToString();
        e.WatchRoot = req.Kind == DriftSnapshotKind.SystemInfo ? "" : req.WatchRoot ?? "";
        e.TakenAtUtc = req.TakenAtUtc;
        e.TreeJson = req.TreeJson;
        e.SystemInfoJson = req.SystemInfoJson;
    }

    private static DriftDiffSnapshot ReqToNew(DriftDiffReq req)
    {
        var e = new DriftDiffSnapshot();
        Apply(req, e);
        return e;
    }

    private static void Apply(DriftDiffReq req, DriftDiffSnapshot e)
    {
        e.InstanceId = req.InstanceId;
        e.FromSnapshotId = req.FromSnapshotId;
        e.ToSnapshotId = req.ToSnapshotId;
        e.Source = req.Source.ToString();
        e.FileChangesJson = req.FileChangesJson;
        e.SystemDifferencesJson = req.SystemDifferencesJson;
    }

    private static DriftChangeEvent ReqToNew(DriftChangeBatchReq req)
    {
        var first = req.Changes.Count > 0 ? req.Changes[0] : new FileSystemChangeDto();
        return new() {
            InstanceId = req.InstanceId,
            WatchRoot = req.WatchRoot ?? "",
            OccurredAtUtc = first.OccurredAtUtc,
            ChangeJson = JsonSerializer.Serialize(first, Json)
        };
    }
}
