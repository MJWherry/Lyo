using System.Diagnostics;
using Lyo.Audiobook.Enums;

namespace Lyo.Audiobook;

/// <summary>A credited person on an audiobook work. <see cref="PersonRef" /> is an opaque EntityRef string, not a People FK.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AudiobookCredit
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public AudiobookCreditRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? PersonRef { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"AudiobookCredit(Name=\"{DisplayName}\", Role={Role})";
}