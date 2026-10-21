using System.Diagnostics;
using Lyo.Book.Enums;

namespace Lyo.Book;

/// <summary>Alternate or translated title for a book series.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookSeriesAlternateTitle
{
    public Guid Id { get; set; }

    public Guid SeriesId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Language { get; set; }

    public TitleKind Kind { get; set; }

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookSeriesAlternateTitle(Id={Id}, Title=\"{Title}\", Kind={Kind})";
}