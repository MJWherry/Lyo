using System.Diagnostics;

namespace Lyo.Audiobook;

/// <summary>A spoken-word work. It may be one audio file, a chapter list, or both. Transcripts are omitted from reads.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Audiobook
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string? Language { get; set; }

    public int? Year { get; set; }

    public int? RuntimeSeconds { get; set; }

    public string? CoverImageRef { get; set; }

    public string? AudioFileRef { get; set; }

    public string? Overview { get; set; }

    public Guid? SeriesId { get; set; }

    public decimal? SeriesIndex { get; set; }

    /// <summary>Plain-text transcript for a single-file work. Omitted from work reads; use <see cref="IAudiobookStore.SearchTranscriptsAsync" />.</summary>
    public string? Transcript { get; set; }

    public IReadOnlyList<Guid> GenreIds { get; set; } = [];

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public IReadOnlyList<AudiobookCredit> Credits { get; set; } = [];

    public IReadOnlyList<AudiobookIdentifier> Identifiers { get; set; } = [];

    public IReadOnlyList<AudiobookChapter> Chapters { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"Audiobook(Id={Id}, Title=\"{Title}\")";
}
