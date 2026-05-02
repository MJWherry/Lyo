using System.ComponentModel.DataAnnotations;
using Lyo.Comic.Enums;

namespace Lyo.Comic.Postgres.Database;

public sealed class SeriesEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Slug { get; set; } = string.Empty;

    public ComicType ComicType { get; set; }

    public ComicStatus Status { get; set; }

    public string? Description { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    public int? PublishedYear { get; set; }

    [MaxLength(500)]
    public string? Author { get; set; }

    [MaxLength(500)]
    public string? Artist { get; set; }

    [MaxLength(500)]
    public string? Publisher { get; set; }

    public string? Source { get; set; }

    public string? CoverImageRef { get; set; }

    [MaxLength(50)]
    public string? Demographic { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ICollection<AlternateTitleEntity> AlternateTitles { get; set; } = [];

    public ICollection<VolumeEntity> Volumes { get; set; } = [];

    public ICollection<ChapterEntity> Chapters { get; set; } = [];

    public ICollection<CharacterEntity> Characters { get; set; } = [];
}