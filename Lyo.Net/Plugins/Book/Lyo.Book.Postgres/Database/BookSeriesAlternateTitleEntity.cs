using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Lyo.Book.Enums;

namespace Lyo.Book.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookSeriesAlternateTitleEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid SeriesId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Language { get; set; }

    public TitleKind Kind { get; set; }

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }

    public BookSeriesEntity Series { get; set; } = null!;

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}