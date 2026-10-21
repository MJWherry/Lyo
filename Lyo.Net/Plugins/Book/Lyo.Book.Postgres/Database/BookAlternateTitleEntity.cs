using System.ComponentModel.DataAnnotations;
using Lyo.Book.Enums;

namespace Lyo.Book.Postgres.Database;

public sealed class BookAlternateTitleEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Language { get; set; }

    public TitleKind Kind { get; set; }

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }

    public BookEntity Book { get; set; } = null!;
}