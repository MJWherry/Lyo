namespace Lyo.Video;

/// <summary>Full-text query over movie and episode transcripts.</summary>
public sealed class VideoTranscriptQuery
{
    /// <summary>Plain-text query passed to <c>plainto_tsquery('english', ...)</c>.</summary>
    public string? FullText { get; set; }
}
