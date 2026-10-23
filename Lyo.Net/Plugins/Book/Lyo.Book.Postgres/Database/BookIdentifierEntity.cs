using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Book.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
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

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}