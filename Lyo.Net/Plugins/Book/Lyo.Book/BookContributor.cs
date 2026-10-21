using System.Diagnostics;
using Lyo.Book.Enums;

namespace Lyo.Book;

/// <summary>A credited person on a book. <see cref="PersonRef" /> is an opaque EntityRef string, not a People FK.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookContributor
{
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public ContributorRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? Language { get; set; }

    public string? PersonRef { get; set; }

    public string? Notes { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookContributor(Id={Id}, Name=\"{DisplayName}\", Role={Role}, Ordinal={Ordinal})";
}