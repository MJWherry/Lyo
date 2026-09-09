using System.ComponentModel.DataAnnotations;

namespace Lyo.FileSystemWatcher.Postgres.Database;

/// <summary>A watched directory and its options.</summary>
public sealed class FileSystemWatchEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(2048)]
    public string RootPath { get; set; } = "";

    /// <summary>Serialized <c>FileSystemWatchOptionsDto</c>.</summary>
    [Required]
    public string OptionsJson { get; set; } = "{}";

    public DateTime CreatedTimestamp { get; set; }

    public List<FileSystemSnapshotEntity> Snapshots { get; set; } = [];

    public List<FileSystemChangeEntity> Changes { get; set; } = [];
}
