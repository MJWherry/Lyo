using System.ComponentModel.DataAnnotations;
using Lyo.Book.Enums;

namespace Lyo.Book.Postgres.Database;

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
}