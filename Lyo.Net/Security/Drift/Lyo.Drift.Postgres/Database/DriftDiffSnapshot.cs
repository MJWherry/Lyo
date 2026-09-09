using System.ComponentModel.DataAnnotations;
using Lyo.Drift.Models;

namespace Lyo.Drift.Postgres.Database;

/// <summary>A computed diff between two structure snapshots.</summary>
public sealed class DriftDiffSnapshot
{
    [Key]
    public Guid Id { get; set; }

    public Guid InstanceId { get; set; }

    public Guid? FromSnapshotId { get; set; }

    public Guid ToSnapshotId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Source { get; set; } = nameof(DriftDiffSource.Agent);

    public DateTime ComputedAtUtc { get; set; }

    public string? FileChangesJson { get; set; }

    public string? SystemDifferencesJson { get; set; }

    public DriftInstance Instance { get; set; } = null!;
}
