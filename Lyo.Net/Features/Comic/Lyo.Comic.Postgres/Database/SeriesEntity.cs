using System.ComponentModel.DataAnnotations;

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

    public int ComicType { get; set; }

    public int Status { get; set; }

    public string? Description { get; set; }

    [MaxLength(10)]
    public string? OriginalLanguage { get; set; }

    public int? PublishedYear { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ICollection<AlternateTitleEntity> AlternateTitles { get; set; } = [];

    public ICollection<VolumeEntity> Volumes { get; set; } = [];

    public ICollection<ChapterEntity> Chapters { get; set; } = [];
}