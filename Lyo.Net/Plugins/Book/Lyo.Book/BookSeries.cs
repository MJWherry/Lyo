using System.Diagnostics;
using Lyo.Book.Enums;

namespace Lyo.Book;

/// <summary>Optional grouping of catalog books (e.g. "Harry Potter").</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookSeries
{
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

    public DateTime? StartedDate { get; set; }

    public DateTime? EndedDate { get; set; }

    public string? CoverImageRef { get; set; }

    public string? Source { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public IReadOnlyList<BookSeriesAlternateTitle> AlternateTitles { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"BookSeries(Id={Id}, Title=\"{Title}\", Slug=\"{Slug}\", Status={Status})";
}
