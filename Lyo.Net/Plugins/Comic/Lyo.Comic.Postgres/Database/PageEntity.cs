using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Comic.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class PageEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid ChapterId { get; set; }

    [Required]
    public int PageNumber { get; set; }

    [MaxLength(512)]
    public string? ImageRef { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ChapterEntity Chapter { get; set; } = null!;

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}