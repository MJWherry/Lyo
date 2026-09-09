using System.ComponentModel.DataAnnotations;

namespace Lyo.FileSystemWatcher.Postgres.Database;

/// <summary>One file-system change event.</summary>
public sealed class FileSystemChangeEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid WatchId { get; set; }

    public Guid? SnapshotId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    [Required]
    [MaxLength(16)]
    public string ChangeType { get; set; } = "";

    public bool IsDirectory { get; set; }

    [MaxLength(2048)]
    public string? OldPath { get; set; }

    [MaxLength(2048)]
    public string? NewPath { get; set; }

    [Required]
    public string ChangeJson { get; set; } = "{}";

    public FileSystemWatchEntity Watch { get; set; } = null!;

    public FileSystemSnapshotEntity? Snapshot { get; set; }
}
