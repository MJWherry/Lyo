using System.ComponentModel.DataAnnotations;
using Lyo.Drift.Models;

namespace Lyo.Drift.Postgres.Database;

/// <summary>One file-tree or system-info structure snapshot. Lineage is instance + kind + watch root.</summary>
public sealed class DriftStructureSnapshot
{
    [Key]
    public Guid Id { get; set; }

    public Guid InstanceId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Kind { get; set; } = nameof(DriftSnapshotKind.FileTree);

    [Required]
    [MaxLength(2048)]
    public string WatchRoot { get; set; } = "";

    public DateTime TakenAtUtc { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    [Required]
    [MaxLength(128)]
    public string ContentHash { get; set; } = "";

    /// <summary>Digest name from <c>ContentDigestAlgorithm</c> (for example <c>Sha256</c>). Compared with <see cref="ContentHash" /> for dedupe.</summary>
    [Required]
    [MaxLength(32)]
    public string ContentHashAlgorithm { get; set; } = "Sha256";

    public string? TreeJson { get; set; }

    public string? SystemInfoJson { get; set; }

    public DriftInstance Instance { get; set; } = null!;
}
