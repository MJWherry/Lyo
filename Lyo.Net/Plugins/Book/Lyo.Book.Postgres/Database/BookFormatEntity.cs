using System.ComponentModel.DataAnnotations;

namespace Lyo.Book.Postgres.Database;

public sealed class BookFormatEntity
{
    [Key]
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public ICollection<BookEntity> Books { get; set; } = [];
}