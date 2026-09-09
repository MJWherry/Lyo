using System.ComponentModel.DataAnnotations;

namespace Lyo.FileSystemWatcher.Postgres.Database;

/// <summary>One structure snapshot of a watch.</summary>
public sealed class FileSystemSnapshotEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid WatchId { get; set; }

    public DateTime TakenAtUtc { get; set; }

    public int FileCount { get; set; }

    public int DirectoryCount { get; set; }

    [Required]
    [MaxLength(128)]
    public string ContentHash { get; set; } = "";

    /// <summary>Digest name from <c>ContentDigestAlgorithm</c> (for example <c>Sha256</c>). Compared with <see cref="ContentHash" /> for dedupe.</summary>
    [Required]
    [MaxLength(32)]
    public string ContentHashAlgorithm { get; set; } = "Sha256";

    [Required]
    public string TreeJson { get; set; } = "{}";

    public FileSystemWatchEntity Watch { get; set; } = null!;

    public List<FileSystemChangeEntity> Changes { get; set; } = [];
}
