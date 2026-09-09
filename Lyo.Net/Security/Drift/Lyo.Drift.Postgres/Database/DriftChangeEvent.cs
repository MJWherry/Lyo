using System.ComponentModel.DataAnnotations;

namespace Lyo.Drift.Postgres.Database;

/// <summary>One live file-system change from an agent batch.</summary>
public sealed class DriftChangeEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid InstanceId { get; set; }

    [Required]
    [MaxLength(2048)]
    public string WatchRoot { get; set; } = "";

    public Guid? SnapshotId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    [Required]
    public string ChangeJson { get; set; } = "{}";

    public DriftInstance Instance { get; set; } = null!;
}
