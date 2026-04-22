using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>An alternate or translated title for a comic series.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicAlternateTitle
{
    /// <summary>Gets or sets the unique identifier of this alternate title.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the series this title belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Gets or sets the alternate title text.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the BCP 47 language tag for this title (e.g. "en", "ja", "ko"), or null if unknown.</summary>
    public string? Language { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicAlternateTitle(Id={Id}, SeriesId={SeriesId}, Title=\"{Title}\", Language={Language ?? "null"})";
}
