using System.ComponentModel.DataAnnotations;

namespace Lyo.Book.Postgres.Database;

public sealed class BookGenreEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public BookGenreEntity? Parent { get; set; }

    public ICollection<BookGenreEntity> Children { get; set; } = [];

    public ICollection<BookBookGenreEntity> BookLinks { get; set; } = [];
}
