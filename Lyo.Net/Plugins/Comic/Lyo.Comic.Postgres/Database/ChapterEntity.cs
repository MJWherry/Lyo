using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Comic.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class ChapterEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid SeriesId { get; set; }

    public Guid? VolumeId { get; set; }

    [Required]
    public decimal ChapterNumber { get; set; }

    [MaxLength(500)]
    public string? Title { get; set; }

    [Required]
    [MaxLength(10)]
    public string Language { get; set; } = string.Empty;

    public int? PageCount { get; set; }

    public DateOnly? PublishedDate { get; set; }

    [MaxLength(512)]
    public string? Source { get; set; }

    [MaxLength(512)]
    public string? CoverImageRef { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public SeriesEntity Series { get; set; } = null!;

    public VolumeEntity? Volume { get; set; }

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}