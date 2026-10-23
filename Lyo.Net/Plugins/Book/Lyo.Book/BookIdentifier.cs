using System.Diagnostics;

namespace Lyo.Book;

/// <summary>Bibliographic or library identifier on a book. Unique catalog-wide on (scheme, normalized value).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookIdentifier
{
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public string? Notes { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookIdentifier(Scheme={Scheme}, Value=\"{Value}\")";
}