namespace Lyo.Drift.Models.Request;

/// <summary>Upsert payload for a drift agent instance.</summary>
public sealed class DriftInstanceReq
{
    /// <summary>Stable key (configured name plus machine). Unique per collector.</summary>
    public string InstanceKey { get; set; } = "";

    public string MachineName { get; set; } = "";

    public int ProcessId { get; set; }

    public DriftInstanceState State { get; set; } = DriftInstanceState.Running;

    /// <summary>JSON summary of configured watches.</summary>
    public string? WatchesJson { get; set; }

    public IReadOnlyDictionary<string, string?>? Metadata { get; set; }
}

/// <summary>Heartbeat patch fields.</summary>
public sealed class DriftInstanceHeartbeatReq
{
    public DateTime LastHeartbeatUtc { get; set; }

    public DriftInstanceState? State { get; set; }
}

/// <summary>Ingest a structure snapshot (file tree and/or system-info projection JSON).</summary>
public sealed class DriftSnapshotReq
{
    public Guid InstanceId { get; set; }

    public DriftSnapshotKind Kind { get; set; }

    /// <summary>Watch root for file-tree snapshots. Null for system-info.</summary>
    public string? WatchRoot { get; set; }

    public DateTime TakenAtUtc { get; set; }

    /// <summary>JSON of <c>FileSystemSnapshotTreeDto</c> when <see cref="Kind" /> is FileTree.</summary>
    public string? TreeJson { get; set; }

    /// <summary>JSON of <c>SystemInfoDriftProjection</c> when <see cref="Kind" /> is SystemInfo.</summary>
    public string? SystemInfoJson { get; set; }
}

/// <summary>Ingest a precomputed diff.</summary>
public sealed class DriftDiffReq
{
    public Guid InstanceId { get; set; }

    public Guid? FromSnapshotId { get; set; }

    public Guid ToSnapshotId { get; set; }

    public DriftDiffSource Source { get; set; } = DriftDiffSource.Agent;

    public string? FileChangesJson { get; set; }

    public string? SystemDifferencesJson { get; set; }
}

/// <summary>Ingest a live change batch.</summary>
public sealed class DriftChangeBatchReq
{
    public Guid InstanceId { get; set; }

    public string? WatchRoot { get; set; }

    public IList<Lyo.FileSystemWatcher.Models.FileSystemChangeDto> Changes { get; set; } = [];
}
