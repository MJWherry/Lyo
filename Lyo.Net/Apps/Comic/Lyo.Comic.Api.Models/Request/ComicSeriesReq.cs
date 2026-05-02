using Lyo.Comic.Enums;

namespace Lyo.Comic.Api.Models.Request;

/// <summary>Request model for creating or updating a comic series.</summary>
public sealed class ComicSeriesReq
{
    /// <summary>Gets or sets the primary display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-friendly slug. Must be unique across all series.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the publication style and region of origin.</summary>
    public ComicType ComicType { get; set; }

    /// <summary>Gets or sets the current publication status.</summary>
    public ComicStatus Status { get; set; }

    /// <summary>Gets or sets a description or synopsis.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the BCP 47 language tag for the original publication language (e.g. "ja", "en").</summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets the year the series was first published.</summary>
    public int? PublishedYear { get; set; }

    /// <summary>Gets or sets the primary author or writer.</summary>
    public string? Author { get; set; }

    /// <summary>Gets or sets the artist, if different from the author.</summary>
    public string? Artist { get; set; }

    /// <summary>Gets or sets the original publisher.</summary>
    public string? Publisher { get; set; }

    /// <summary>Gets or sets the source URL or attribution string.</summary>
    public string? Source { get; set; }

    /// <summary>Gets or sets a reference to the series cover image in file storage.</summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Gets or sets the target demographic (e.g. "Shonen", "Seinen").</summary>
    public string? Demographic { get; set; }

    /// <summary>Gets or sets alternate and translated titles for this series.</summary>
    public IReadOnlyList<ComicAlternateTitleReq> AlternateTitles { get; set; } = [];
}

/// <summary>An alternate or translated title within a series request.</summary>
public sealed class ComicAlternateTitleReq
{
    /// <summary>Gets or sets the alternate title text.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the BCP 47 language tag for this title, or null if unknown.</summary>
    public string? Language { get; set; }
}