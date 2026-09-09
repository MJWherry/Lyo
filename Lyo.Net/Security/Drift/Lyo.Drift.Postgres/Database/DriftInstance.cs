using System.ComponentModel.DataAnnotations;
using Lyo.Drift.Models;

namespace Lyo.Drift.Postgres.Database;

/// <summary>A registered drift agent process. Upserted by <see cref="DriftInstance.InstanceKey" /> so re-register does not split snapshot history.</summary>
public sealed class DriftInstance
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string InstanceKey { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string MachineName { get; set; } = "";

    public int ProcessId { get; set; }

    [Required]
    [MaxLength(32)]
    public string State { get; set; } = nameof(DriftInstanceState.Running);

    public DateTime LastHeartbeatUtc { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public string? WatchesJson { get; set; }

    public string? MetadataJson { get; set; }

    public List<DriftStructureSnapshot> Snapshots { get; set; } = [];

    public List<DriftDiffSnapshot> Diffs { get; set; } = [];

    public List<DriftChangeEvent> Changes { get; set; } = [];
}
