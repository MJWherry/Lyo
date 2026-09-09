namespace Lyo.Drift.Models.Response;

/// <summary>A registered drift agent instance.</summary>
public sealed class DriftInstanceRes
{
    public Guid Id { get; set; }
    public string InstanceKey { get; set; } = "";
    public string MachineName { get; set; } = "";
    public int ProcessId { get; set; }
    public DriftInstanceState State { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
    public string? WatchesJson { get; set; }
    public string? MetadataJson { get; set; }
}

/// <summary>A stored structure snapshot.</summary>
public sealed class DriftSnapshotRes
{
    public Guid Id { get; set; }
    public Guid InstanceId { get; set; }
    public DriftSnapshotKind Kind { get; set; }
    public string? WatchRoot { get; set; }
    public DateTime TakenAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public string ContentHash { get; set; } = "";

    /// <summary>Digest name used to produce <see cref="ContentHash" /> (for example <c>Sha256</c>).</summary>
    public string ContentHashAlgorithm { get; set; } = "Sha256";

    public bool Deduped { get; set; }
    public string? TreeJson { get; set; }
    public string? SystemInfoJson { get; set; }
}

/// <summary>A stored diff snapshot.</summary>
public sealed class DriftDiffRes
{
    public Guid Id { get; set; }
    public Guid InstanceId { get; set; }
    public Guid? FromSnapshotId { get; set; }
    public Guid ToSnapshotId { get; set; }
    public DriftDiffSource Source { get; set; }
    public DateTime ComputedAtUtc { get; set; }
    public string? FileChangesJson { get; set; }
    public string? SystemDifferencesJson { get; set; }
}

/// <summary>A stored live change event.</summary>
public sealed class DriftChangeRes
{
    public Guid Id { get; set; }
    public Guid InstanceId { get; set; }
    public string? WatchRoot { get; set; }
    public Guid? SnapshotId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Lyo.FileSystemWatcher.Models.FileSystemChangeDto Change { get; set; } = new();
}
