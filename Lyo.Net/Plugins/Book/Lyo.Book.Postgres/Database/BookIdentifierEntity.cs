using System.ComponentModel.DataAnnotations;

namespace Lyo.Book.Postgres.Database;

public sealed class BookIdentifierEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public string? Notes { get; set; }

    public BookEntity Book { get; set; } = null!;
}
