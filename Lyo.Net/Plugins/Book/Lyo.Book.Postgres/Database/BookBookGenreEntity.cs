using System.Diagnostics;

namespace Lyo.Book.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookBookGenreEntity
{
    public Guid BookId { get; set; }

    public Guid GenreId { get; set; }

    public BookEntity Book { get; set; } = null!;

    public BookGenreEntity Genre { get; set; } = null!;

    /// <summary>Type name and primary key.</summary>
    public override string ToString() => $"{GetType().Name} {BookId} {GenreId}";
}