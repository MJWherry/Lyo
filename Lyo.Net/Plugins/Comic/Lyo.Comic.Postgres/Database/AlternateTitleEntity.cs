using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Comic.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class AlternateTitleEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid SeriesId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Language { get; set; }

    public SeriesEntity Series { get; set; } = null!;

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}