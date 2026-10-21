namespace Lyo.Book.Postgres.Database;

public sealed class BookBookGenreEntity
{
    public Guid BookId { get; set; }

    public Guid GenreId { get; set; }

    public BookEntity Book { get; set; } = null!;

    public BookGenreEntity Genre { get; set; } = null!;
}