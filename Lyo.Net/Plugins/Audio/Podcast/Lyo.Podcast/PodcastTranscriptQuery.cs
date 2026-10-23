namespace Lyo.Podcast;

/// <summary>Full-text query over episode transcripts.</summary>
public sealed class PodcastTranscriptQuery
{
    /// <summary>Plain-text query passed to <c>plainto_tsquery('english', ...)</c>.</summary>
    public string? FullText { get; set; }
}