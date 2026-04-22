using System.Diagnostics;
using Lyo.Comic.Enums;

namespace Lyo.Comic;

/// <summary>Represents a comic series (the top-level title, e.g. "One Piece", "Batman").</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicSeries
{
    /// <summary>Gets or sets the unique identifier of the series.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the primary display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-friendly slug, unique across all series.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the publication style and region of origin.</summary>
    public ComicType ComicType { get; set; }

    /// <summary>Gets or sets the current publication status.</summary>
    public ComicStatus Status { get; set; }

    /// <summary>Gets or sets a description / synopsis of the series.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the BCP 47 language tag for the original publication language (e.g. "ja", "en", "ko", "zh").</summary>
    public string? OriginalLanguage { get; set; }

    /// <summary>Gets or sets the year the series was first published.</summary>
    public int? PublishedYear { get; set; }

    /// <summary>Gets or sets when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets or sets the alternate and translated titles for this series.</summary>
    public IReadOnlyList<ComicAlternateTitle> AlternateTitles { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"ComicSeries(Id={Id}, Title=\"{Title}\", Slug=\"{Slug}\", Type={ComicType}, Status={Status})";
}