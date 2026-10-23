using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.FileSystemWatcher.Postgres.Database;

/// <summary>A watched directory and its options.</summary>
[DebuggerDisplay("{ToString(),nq}")]
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

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}