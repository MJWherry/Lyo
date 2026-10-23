using System.Diagnostics;

namespace Lyo.Book;

/// <summary>A chapter of a catalog book. Per-language rows share a chapter number.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookChapter
{
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

    public DateTime? PublishedDate { get; set; }

    public string? CoverImageRef { get; set; }

    public string? Source { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookChapter(Id={Id}, BookId={BookId}, Chapter={ChapterNumber}, Language=\"{Language}\")";
}