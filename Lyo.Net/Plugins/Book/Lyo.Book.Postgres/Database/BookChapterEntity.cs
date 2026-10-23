using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Book.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookChapterEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public decimal ChapterNumber { get; set; }

    public string? Title { get; set; }

    public string? Subtitle { get; set; }

    public string Language { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public int? StartPrintPage { get; set; }

    public int? EndPrintPage { get; set; }

    public int? PageCount { get; set; }

    public int? WordCount { get; set; }

    public string? Summary { get; set; }

    public DateOnly? PublishedDate { get; set; }

    public string? CoverImageRef { get; set; }

    public string? Source { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public BookEntity Book { get; set; } = null!;

    public ICollection<BookPageEntity> Pages { get; set; } = [];

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}