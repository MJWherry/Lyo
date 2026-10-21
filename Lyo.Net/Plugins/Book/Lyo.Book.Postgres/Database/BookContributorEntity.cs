using System.ComponentModel.DataAnnotations;
using Lyo.Book.Enums;

namespace Lyo.Book.Postgres.Database;

public sealed class BookContributorEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public ContributorRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? Language { get; set; }

    public string? PersonRef { get; set; }

    public string? Notes { get; set; }

    public BookEntity Book { get; set; } = null!;
}