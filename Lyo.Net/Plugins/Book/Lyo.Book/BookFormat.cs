using System.Diagnostics;

namespace Lyo.Book;

/// <summary>Lookup row for the publication format (hardcover, ebook, …).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookFormat
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookFormat(Id={Id}, Slug=\"{Slug}\")";
}