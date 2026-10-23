namespace Lyo.Audiobook;

/// <summary>Full-text query over work and chapter transcripts.</summary>
public sealed class AudiobookTranscriptQuery
{
    /// <summary>Plain-text query passed to <c>plainto_tsquery('english', ...)</c>.</summary>
    public string? FullText { get; set; }
}