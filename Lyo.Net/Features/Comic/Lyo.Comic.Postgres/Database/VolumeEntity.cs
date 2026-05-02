using System.ComponentModel.DataAnnotations;

namespace Lyo.Comic.Postgres.Database;

public sealed class VolumeEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid SeriesId { get; set; }

    public decimal? VolumeNumber { get; set; }

    [MaxLength(500)]
    public string? Title { get; set; }

    public string? CoverImageRef { get; set; }

    public DateOnly? PublishedDate { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public SeriesEntity Series { get; set; } = null!;

    public ICollection<ChapterEntity> Chapters { get; set; } = [];

    public ICollection<CharacterEntity> Characters { get; set; } = [];
}