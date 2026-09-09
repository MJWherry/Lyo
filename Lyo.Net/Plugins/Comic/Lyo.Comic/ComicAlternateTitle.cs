using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>A comic series an alternate or translated title.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicAlternateTitle
{
    /// <summary>Value of the unique identifier of this alternate title.</summary>
    public Guid Id { get; set; }

    /// <summary>Value of the series this title belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Alternate title text for this record.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>BCP 47 language tag for this title (e.g. "en", "ja", "ko"), or null if unknown.</summary>
    public string? Language { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicAlternateTitle(Id={Id}, SeriesId={SeriesId}, Title=\"{Title}\", Language={Language ?? "null"})";
}