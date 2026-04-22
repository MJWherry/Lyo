using System.ComponentModel.DataAnnotations;

namespace Lyo.Comic.Postgres.Database;

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
}
