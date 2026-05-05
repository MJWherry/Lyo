using System.ComponentModel.DataAnnotations;

namespace Lyo.Comic.Postgres.Database;

public sealed class CharacterEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid SeriesId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(8192)]
    public string? Description { get; set; }

    [MaxLength(512)]
    public string? ImageRef { get; set; }

    [MaxLength(50)]
    public string? Role { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public SeriesEntity Series { get; set; } = null!;

    public ICollection<VolumeEntity> Volumes { get; set; } = [];
}