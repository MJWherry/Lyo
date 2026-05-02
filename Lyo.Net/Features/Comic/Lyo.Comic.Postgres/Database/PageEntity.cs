using System.ComponentModel.DataAnnotations;

namespace Lyo.Comic.Postgres.Database;

public sealed class PageEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid ChapterId { get; set; }

    [Required]
    public int PageNumber { get; set; }

    public string? ImageRef { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ChapterEntity Chapter { get; set; } = null!;
}