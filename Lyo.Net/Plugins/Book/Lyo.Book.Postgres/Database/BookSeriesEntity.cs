using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Lyo.Book.Enums;

namespace Lyo.Book.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookSeriesEntity
{
    [Key]
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string? Description { get; set; }

    public string? Language { get; set; }

    public string? OriginalLanguage { get; set; }

    public string? Publisher { get; set; }

    public string? Imprint { get; set; }

    public BookSeriesStatus Status { get; set; }

    public int? PlannedBookCount { get; set; }

    public DateOnly? StartedDate { get; set; }

    public DateOnly? EndedDate { get; set; }

    public string? CoverImageRef { get; set; }

    public string? Source { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ICollection<BookSeriesAlternateTitleEntity> AlternateTitles { get; set; } = [];

    public ICollection<BookEntity> Books { get; set; } = [];

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}