using System.Diagnostics;

namespace Lyo.Audiobook;

/// <summary>A chapter of an audiobook. Number is unique per work. Transcripts are omitted from work reads.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AudiobookChapter
{
    public Guid Id { get; set; }

    public Guid WorkId { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? StartSeconds { get; set; }

    public int? DurationSeconds { get; set; }

    public string? AudioFileRef { get; set; }

    /// <summary>Plain-text transcript. Omitted from work reads; use <see cref="IAudiobookStore.SearchTranscriptsAsync" />.</summary>
    public string? Transcript { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"AudiobookChapter(Number={Number}, Title=\"{Title}\")";
}
